using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileVarsEditor
{
    public partial class FormNewVar : Form
    {
        string path = "";
        public FormNewVar(string path, string suggestName, string suggestValue= "")
        {
            InitializeComponent();
            this.path = path;
            tbName.Text = suggestName;
            tbValue.Text = suggestValue;
            tbName.Focus();
            if (tbName.Text.Contains('.'))
            {
                tbName.SelectionStart = tbName.Text.LastIndexOf('.')+1;
                tbName.SelectionLength = tbName.Text.Length - tbName.Text.LastIndexOf('.')-1;
            }
            else
                tbName.SelectAll();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((path.Length > 0) && (path[path.Length-1] != '\\'))
                path += "\\";
            System.IO.File.WriteAllText(path + tbName.Text, tbValue.Text);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
