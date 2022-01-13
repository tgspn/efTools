using System;

namespace Tools.Templates.SQL.Models
{
    public class InstaceClassProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }
        public string Size { get; set; }
        public bool IsPK { get; set; }
        public string TypeCSharp
        {
            get
            {
                var type = string.Empty;
                switch (Type.ToLower().Trim())
                {
                    case "nvarchar":
                    case "nchar":
                    case "varchar":
                        return "string";
                    case "datetime":
                    case "date":
                        type = "DateTime";
                        break;
                    case "bigint":
                        type = "long";
                        break;
                    case "bit":
                        type = "bool";
                        break;
                    default:
                        type = Type;
                        break;

                }
                return $"{type}{(IsNullable ? "?" : "")}";
            }
        }
    }
}
