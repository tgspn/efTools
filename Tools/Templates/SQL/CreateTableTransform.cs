using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tools.Templates.SQL.Models;

namespace Tools.Templates.SQL
{
    public class CreateTableTransform
    {
        public static string ToClass(IEnumerable<string> linhas)
        {
            List<InstanceClass> classes = new List<InstanceClass>();
            var newClass = new InstanceClass();
            foreach (var item in linhas)
            {
                var className = Regex.Match(item, @"create table (\[*dbo\]*.|)\[*(?'class'.*)\]", RegexOptions.IgnoreCase);
                var property = Regex.Match(item, @"\s*\[*(?'name'.*?)\]* \[*(?'type'.*?)\]*(?'size'|\(.*?\))\s*(?'pk'|IDENTITY\(.*\) )(?'nullable'NOT NULL|NULL)", RegexOptions.IgnoreCase);
                if (className.Success)
                {
                    newClass.ClassName = className.Groups["class"].Value;
                    classes.Add(newClass);
                }
                if (property.Success)
                {
                    newClass.Properties.Add(new InstaceClassProperty()
                    {
                        Name = property.Groups["name"].Value,
                        IsNullable = !Regex.IsMatch(property.Groups["nullable"].Value, "NOT NULL", RegexOptions.IgnoreCase),
                        Type = property.Groups["type"].Value,
                        IsPK = !string.IsNullOrEmpty(property.Groups["pk"].Value),
                        Size = Regex.Replace(property.Groups["size"].Value, "[()]", "")
                    });
                }
            }
            return ToClass(classes);
        }
        public static string ToClass(List<InstanceClass> classes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in classes)
            {
                sb.AppendLine($"public class {item.ClassName}");
                sb.AppendLine("{");
                foreach (var props in item.Properties)
                {
                    if (!props.IsNullable && props.Type.ToLower() == "nvarchar" && !string.IsNullOrEmpty(props.Size))
                        sb.AppendLine($"\t\t[StringLength({props.Size}),Required]");
                    else if (props.IsNullable && props.Type.ToLower() == "nvarchar" && !string.IsNullOrEmpty(props.Size))
                        sb.AppendLine($"\t\t[StringLength({props.Size})]");
                    else if (!props.IsNullable && Regex.IsMatch(props.Type, "(n|var)char", RegexOptions.IgnoreCase) && !string.IsNullOrEmpty(props.Size))
                        sb.AppendLine($"\t\t[Column(TypeName = \"{props.Type}({props.Size})\"), Required]");
                    else if (props.IsNullable && Regex.IsMatch(props.Type, "(n|var)char", RegexOptions.IgnoreCase) && !string.IsNullOrEmpty(props.Size))
                        sb.AppendLine($"\t\t[Column(TypeName = \"{props.Type}({props.Size})\")]");
                    else if (!props.IsNullable && Regex.IsMatch(props.Type, "n(|var)char", RegexOptions.IgnoreCase))
                        sb.AppendLine($"\t\t[Required]");
                    else if (props.Type == "date")
                        sb.AppendLine($"\t\t[Column(TypeName = \"{props.Type})\")]");
                    if (props.IsPK)
                    {
                        sb.AppendLine($"\t\t[Key]");
                        sb.AppendLine($"[Column(\"{props.Name}\")]");
                        sb.AppendLine($"\t\tpublic {props.TypeCSharp} Id {{ get; set; }}");
                    }
                    else
                    {
                        sb.AppendLine($"\t\tpublic {props.TypeCSharp} {props.Name} {{ get; set; }}");
                    }
                }

                foreach (var props in item.Properties.Where(x => x.Name.Trim().StartsWith("Cod")))
                {
                    sb.AppendLine($"\t\t[ForeignKey(\"{props.Name}\")]");
                    sb.AppendLine($"\t\tpublic {props.Name.Replace("Cod", "")}Entity {props.Name.Replace("Cod", "")} {{ get; set; }}");
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string ToDbSet(IEnumerable<string> linhas)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in linhas)
            {
                var className = Regex.Match(item, @"create table (\[*dbo\]*.|)\[*(?'class'.*)\]", RegexOptions.IgnoreCase);
                if (className.Success)
                {
                    var name = className.Groups["class"].Value;
                    sb.AppendLine($"public DbSet<{name}Entity> {name} {{ get; set; }}");
                }
            }
            return sb.ToString();
        }
        public static string ToTypeConfiguration(IEnumerable<string> linhas, bool addIdIfNot)
        {
            var inserts = GetInsertDataModel(linhas);

            return ToTypeConfiguration(inserts, addIdIfNot);
        }
        public static List<InsertDataModel> GetInsertDataModel(IEnumerable<string> linhas)
        {
            var inserts = new List<InsertDataModel>();

            foreach (var item in linhas)
            {
                var insertMatch = Regex.Match(item, @"^INSERT (INTO)*\s*( |\[dbo\]\.|dbo\.|)\[*(?'class'.*?)\]* ", RegexOptions.IgnoreCase);
                if (insertMatch.Success)
                {
                    var name = insertMatch.Groups["class"].Value;
                    var colsMatch = Regex.Match(item, @"\((?'cols'\[*.*?\]*)\) ");
                    var valuesMatch = Regex.Match(item, @"VALUES\s*\((?'values'.*)\)");
                    if (valuesMatch.Success)
                    {
                        var matches = Regex.Matches(colsMatch.Groups["cols"].Value, @"(\[(.*?)\]|\w+)").Select(x => x.Groups[1].Value).ToArray();
                        var values = Regex.Split(valuesMatch.Groups["values"].Value, @",(?=(?:[^\']*\'[^\']*\')*[^\']*$)").ToArray();
                        var dic = new Dictionary<string, string>();
                        for (int i = 0; i < matches.Length; i++)
                        {
                            try
                            {
                                dic[matches[i]] = GetValue(values[i]);
                            }
                            catch
                            {

                            }
                        }
                        var data = new InsertDataModel()
                        {
                            ClassName = name,
                            Data = dic
                        };
                        inserts.Add(data);
                    }
                }
            }
            return inserts;
        }
        public static string ToTypeConfiguration(List<InsertDataModel> datas, bool addIdIfNot)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var groups in datas.GroupBy(x => x.ClassName))
            {

                int id = 1;
                sb.AppendLine($"public class {groups.Key}EntityTypeConfiguration : IEntityTypeConfiguration<{groups.Key}Entity>");
                sb.AppendLine("{");
                sb.AppendLine($"\t\tpublic void Configure(EntityTypeBuilder<{groups.Key}Entity> builder)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\t\tInsertDefaults(builder);");
                sb.AppendLine("\t\t}");
                sb.AppendLine();

                sb.AppendLine($"\t\tpublic void InsertDefaults(EntityTypeBuilder<{groups.Key}Entity> builder)");
                sb.AppendLine("\t\t{");
                foreach (var item in groups)
                {
                    sb.AppendLine($"\t\t\t\tbuilder.HasData(new {item.ClassName}Entity()");
                    sb.AppendLine("\t\t\t\t\t\t{");
                    if (addIdIfNot)
                        sb.AppendLine($"\t\t\t\t\t\t\t\tId = {id++},");
                    foreach (var keyValue in item.Data)
                    {
                        if (keyValue.Value != "NULL")
                        {
                            sb.AppendLine($"\t\t\t\t\t\t\t\t{keyValue.Key} = {keyValue.Value},");
                        }
                    }
                    sb.AppendLine("\t\t\t\t\t\t});");
                    sb.AppendLine();
                }
                sb.AppendLine("\t\t}");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        public static string ToApplyConfiguration(List<InsertDataModel> datas)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var groups in datas.GroupBy(x => x.ClassName))
            {
                sb.AppendLine($"modelBuilder.ApplyConfiguration(new {groups.Key}EntityTypeConfiguration());");
            }

            return sb.ToString();
        }
        private static string GetValue(string v)
        {
            if (Regex.IsMatch(v, @"N*'"))
                return $"\"{Regex.Replace(v.Trim().Replace("\\", "\\\\"), @"N*'", "")}\"";
            return v;
        }
        public class InsertDataModel
        {
            public string ClassName { get; set; }
            public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        }
    }
}
