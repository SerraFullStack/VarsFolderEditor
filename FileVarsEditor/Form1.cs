using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileVarsEditor
{
    public partial class Form1 : Form
    {
        TreeNode editingNode;
        string currengGloablDbPath;
        private object dynamic;

        public Form1()
        {
            InitializeComponent();
        }

        public void loadFolder(string globalDbPath)
        {
            loadGlobalDb(globalDbPath);
            currengGloablDbPath = globalDbPath;
        }

        public void loadGlobalDb(string globalDbPath)
        {
            treeView1.Nodes.Clear();
            //lista os arquivos
            string[] files = Directory.GetFiles(globalDbPath);

            //define a lista de nodos atual como sendo o nodo raíz do treeview
            var parentAtt = treeView1.Nodes;

            progressBar1.Maximum = files.Length;
            progressBar1.Value = 0;
            progressBar1.Show();
            treeView1.Hide();
            foreach (var attFname in files)
            {
                parentAtt = treeView1.Nodes;

                progressBar1.Value = progressBar1.Value + 1;
                var att = Path.GetFileName(attFname);
                string[] names = att.Split('.');
                string completeName = globalDbPath + "\\";
                foreach (var nameAtt in names)
                {
                    completeName += nameAtt;
                    //verifica se o nome atual já está na lista parent
                    if (parentAtt.ContainsKey(completeName))
                    {
                        //define o nodo encontrado como o parentAtt
                        parentAtt = parentAtt[parentAtt.IndexOfKey(completeName)].Nodes;
                    }
                    else
                    {
                        //adiciona o nodo na lista e o define como parent
                        parentAtt = parentAtt.Add(completeName, nameAtt).Nodes;
                    }
                    completeName += '.';
                    //Application.DoEvents();
                }
            }
            progressBar1.Hide();
            treeView1.Show();
        }

        private void editNode(TreeNode node)
        {
            if (File.Exists(node.Name))
            {
                textBox1.Text = File.ReadAllText(node.Name);
                btDelete.Enabled = true;
                btReload.Enabled = true;

                FileInfo fI = new FileInfo(node.Name);
                lbCreatedAt.Text = "Criada em: " + fI.CreationTime.ToString();
                lbModifiedAt.Text = "Modificada em: " + fI.LastWriteTime.ToString();
                fI = null;
            }
            else
            {
                textBox1.Clear();
                btDelete.Enabled = false;
                btReload.Enabled = false;
                lbCreatedAt.Text = "Criada em: ";
                lbModifiedAt.Text = "Modificada em: ";
            }

            editingNode = node;
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            loadGlobalDb(currengGloablDbPath);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Deseja realmente apagar esta chave e todos suas subchaves?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                //percore recursivamente a chave
                this.delNode(editingNode);

                //recarrega o banco
                //loadGlobalDb(currengGloablDbPath);
            }
        }

        private void btDelete_Click(object sender, EventArgs e)
        {
            if (File.Exists(editingNode.Name))
            {
                File.Delete(editingNode.Name);

                //se for uma folha na arvore, remove o nodo de lá também
                while ((!(File.Exists(editingNode.Name))) && (editingNode.Nodes.Count == 0))
                {
                    var currNode = editingNode.Parent;

                    if (currNode == null)
                    {
                        if (treeView1.Nodes.Contains(editingNode))
                            treeView1.Nodes.Remove(editingNode);
                        break;
                    }

                    currNode.Nodes.Remove(editingNode);
                    editingNode = currNode;
                }

            }

            editNode(editingNode);
        }

        private void btReload_Click(object sender, EventArgs e)
        {
            editNode(editingNode);
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            File.WriteAllText(editingNode.Name, textBox1.Text);
            editNode(editingNode);
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            editNode(e.Node);
        }

        private void delNode(TreeNode node)
        {
            if (node.Nodes.Count > 0)
            {
                while (node.Nodes.Count > 0)
                    delNode(node.Nodes[0]);
            }

            var parentNode = node.Parent;
            if (File.Exists(node.Name))
            {
                File.Delete(node.Name);
            }

            if (parentNode != null)
            {
                if (parentNode.Nodes.Contains(node))
                    parentNode.Nodes.Remove(node);
            }
            else
            {
                if (treeView1.Nodes.Contains(node))
                    treeView1.Nodes.Remove(node);
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void abrirPastaDeArquivosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog abrir = new FolderBrowserDialog();
            if (abrir.ShowDialog() == DialogResult.OK)
            {
                this.loadFolder(abrir.SelectedPath);
            }
        }

        private void digitarOEndereçoDaPastaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            
        }

        private void menuStrip1_ItemClicked_1(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void toolStripTextBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                loadFolder(toolStripTextBox2.Text);
        }

        int remainThreads = 0;
        private void exportarOBancoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            if (save.ShowDialog() == DialogResult.OK)
            {
                Thread tr = new Thread(delegate()
                {
                    ImporterExporter.ImporterExporter exporter = new ImporterExporter.ImporterExporter();
                    this.Invoke((MethodInvoker)delegate () { progressBar1.Visible = true; });
                    exporter.export(this.currengGloablDbPath, save.FileName, delegate (int max, int att)
                    {
                        this.Invoke((MethodInvoker)delegate ()
                        {
                            progressBar1.Maximum = max;
                            progressBar1.Value = att;

                        });
                    });
                    this.Invoke((MethodInvoker)delegate ()
                    {
                        progressBar1.Visible = false;
                        MessageBox.Show("Banco exportado com sucesso.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                });
                tr.Start();

            }
        }

        private void importarUmBancoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                Thread tr = new Thread(delegate ()
                {
                    ImporterExporter.ImporterExporter importer = new ImporterExporter.ImporterExporter();
                    this.Invoke((MethodInvoker)delegate () { progressBar1.Visible = true; });
                    importer.import(open.FileName, this.currengGloablDbPath, delegate (int max, int att)
                    {
                        this.Invoke((MethodInvoker)delegate ()
                        {
                            progressBar1.Maximum = max;
                            progressBar1.Value = att;

                        });
                    });
                    this.Invoke((MethodInvoker)delegate () 
                    {
                        progressBar1.Visible = false;
                        MessageBox.Show("Banco importado com sucesso.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        loadGlobalDb(currengGloablDbPath);
                    });

                });
                tr.Start();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string name = "";
            string originalEditionNode = "";
            if (editingNode != null)
            {
                originalEditionNode = editingNode.Name;
                name = Path.GetFileName(editingNode.Name);
            }
            FormNewVar nv = new FormNewVar(currengGloablDbPath, name, "");
            nv.ShowDialog();

            loadGlobalDb(currengGloablDbPath);

            if (originalEditionNode != "")
            {
                TreeNode[] temp = treeView1.Nodes.Find(originalEditionNode, true);

                if (temp.Length > 0)
                    temp[0].TreeView.SelectedNode = temp[0];
            }

        }
    }

    public abstract class Prompt
    {
        
    }
}
