using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tools.Templates.SQL;
using Tools.Templates.SQL.Models;

namespace Tools
{
    public enum TemplateType
    {
        None,
        CreateTableToClass,
        CreateTableToDbSet,
        InsertToTypeConfiguration,
        InsertToApplyConfiguration
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Dictionary<TemplateType, string> dic = new()
            {
                { TemplateType.None, "Selecione um Template" },
                { TemplateType.CreateTableToClass, "Create Table para Classe" },
                { TemplateType.CreateTableToDbSet, "Create Table para DbSet<>" },
                { TemplateType.InsertToTypeConfiguration, "Transformar insert em classes de configuração" },
                { TemplateType.InsertToApplyConfiguration, "Transformar insert em ApplyConfiguration" }
            };
            comboBox1.DataSource = new BindingSource(dic, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var select = (comboBox1.SelectedValue as TemplateType?) ?? TemplateType.None;
            switch (select)
            {
                case TemplateType.CreateTableToClass:
                    rtbTo.Text = CreateTableTransform.ToClass(rtbFrom.Lines);
                    break;
                case TemplateType.CreateTableToDbSet:
                    rtbTo.Text = CreateTableTransform.ToDbSet(rtbFrom.Lines);
                    break;
                case TemplateType.InsertToTypeConfiguration:
                    rtbTo.Text = CreateTableTransform.ToTypeConfiguration(rtbFrom.Lines,cbxAddId.Checked);
                    break;
                case TemplateType.InsertToApplyConfiguration:
                    rtbTo.Text = CreateTableTransform.ToApplyConfiguration(CreateTableTransform.GetInsertDataModel(rtbFrom.Lines));
                    break;
                case TemplateType.None:
                default:
                    break;
            }

        }
    }
}
