using FilesVars;
using Libs;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        Vars vars = new Vars();
        

        public Form1()
        {
            InitializeComponent();
            
        }

        public void loadFolder(string globalDbPath)
        {
            loadGlobalDb(globalDbPath);
            currengGloablDbPath = globalDbPath;

            int cont = vars.get("lastFolders.count", 0).AsInt;

            if (vars.get("lastFolders." + (cont - 1).ToString(), "").AsString != globalDbPath)
            {
                vars.set("lastFolders." + cont, globalDbPath);
                vars.set("lastFolders.count", (cont+1).ToString());
            }

            this.loadLastFoldersList();
        }

        private void loadLastFoldersList()
        {
            int cont = vars.get("lastFolders.count", 0).AsInt-1;
            int loadeds = 0;
            recentesToolStripMenuItem.DropDownItems.Clear();

            List<string> addedToMenu = new List<string>();

            while ((loadeds < 10) && (cont >= 0))
            {
                string currName = vars.get("lastFolders." + cont.ToString(), "").AsString;
                if (!addedToMenu.Contains(currName))
                {
                    addedToMenu.Add(currName);
                    EasyThread.StartNew(delegate (EasyThread thPointer, object arguments)
                    {
                        string curr = (string)((object[])arguments)[1];


                        //recentesToolStripMenuItem
                        try
                        {

                            if (Directory.Exists(curr))
                            {

                                ToolStripMenuItem temp = new ToolStripMenuItem(curr);
                                temp.Click += delegate (object sender, EventArgs e)
                                {
                                    this.loadFolder(((ToolStripMenuItem)sender).Text);
                                };
                                this.Invoke((MethodInvoker)delegate ()
                                {
                                    recentesToolStripMenuItem.DropDownItems.Add(temp);
                                });
                            }

                        }
                        catch { }
                    }, false, new object[] { cont, currName });
                }
                cont--;
            }
        }


        private void ivk(Action ac)
        {
            this.Invoke((MethodInvoker)delegate() { ac(); });
        }
        public void loadGlobalDb(string globalDbPath)
        {
            EasyThread.StartNew(delegate (EasyThread sender, object arguments)
            {
                

                //define a lista de nodos atual como sendo o nodo raíz do treeview
                var parentAtt = treeView1.Nodes;

                ivk(delegate ()
                {
                    progressBar1.Maximum = 1;
                    progressBar1.Value = 0;
                    progressBar1.Show();
                });
                try
                {
                    //lista os arquivos
                    string[] files = Directory.GetFiles(globalDbPath);
                    
                    ivk(delegate ()
                    {
                        treeView1.Nodes.Clear();
                        progressBar1.Maximum = files.Length;
                        //treeView1.Hide();
                    });

                    foreach (var attFname in files)
                    {
                        ivk(delegate ()
                        {
                            parentAtt = treeView1.Nodes;
                            progressBar1.Value = progressBar1.Value + 1;
                        });

                        var att = Path.GetFileName(attFname);
                        string[] names = att.Split('.');
                        string completeName = globalDbPath + "\\";
                        
                        foreach (var nameAtt in names)
                        {
                            completeName += nameAtt;
                            
                            //verifica se o nome atual já está na lista parent
                            ivk(delegate ()
                            {
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
                            });
                            completeName += '.';
                            //Application.DoEvents();
                        }
                    }

                    ivk(delegate ()
                    {
                        treeView1.TreeViewNodeSorter = new NodeSorter();
                        treeView1.Sort();
                    });
                }
                catch { }
                ivk(delegate ()
                {
                    progressBar1.Hide();
                    //treeView1.Show();
                });
            }, false);
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

        /*private void delNode(TreeNode node)
        {

            

           // Application.DoEvents();
            if (node.Nodes.Count > 0)
            {
                while (node.Nodes.Count > 0)
                    delNode(node.Nodes[0]);
            }

            var parentNode = node.Parent;
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
            
            if (File.Exists(node.Name))
            {
                File.Delete(node.Name);
            }


            
        }*/

        private void delNode(TreeNode node)
        {

            var parentNode = node.Parent;
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


            EasyThread.StartNew(delegate (EasyThread sender, object args)
            {
                if (File.Exists(node.Name))
                {
                    File.Delete(node.Name);
                }

                var childs = new string[] { };

                ivk(delegate ()
                {
                    childs = Directory.GetFiles(Path.GetDirectoryName(node.Name), Path.GetFileName(node.Name) + "*");
                    progressBar1.Maximum = childs.Length;
                    progressBar1.Value = 0;
                    progressBar1.Show();
                });

                Parallel.ForEach(childs, delegate (string curr) {
                    try { File.Delete(curr); } catch { }
                    ivk(delegate ()
                    {
                        try { progressBar1.Value = progressBar1.Value + 1; } catch { }
                    });
                });

                ivk(delegate ()
                {
                    progressBar1.Hide();
                });
            }, false);



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

        private void treeView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                button2_Click(sender, e);
            }
            else if (e.KeyCode == Keys.E)
            {
                editKey();
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.N)
                {
                    button3_Click(sender, e);
                }
                else if (e.KeyCode == Keys.S)
                {
                    btSave_Click(sender, e);
                }
            }
            else if (e.KeyCode == Keys.F5)
            {
                btReload_Click(sender, e);
            }
        }

        public void editKey(string KeyName = "")
        {
            if (KeyName == "")
                KeyName = Path.GetFileName(editingNode.Name);

            string text = "";
            if (File.Exists(editingNode.Name))
                text = File.ReadAllText(editingNode.Name);


            FormNewVar nv = new FormNewVar(currengGloablDbPath, KeyName, text);
            nv.ShowDialog();
            if (nv.DialogResult == DialogResult.OK)
            {
                File.Delete(editingNode.Name);

                loadGlobalDb(currengGloablDbPath);
            }



        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
            Thread th = new Thread(delegate ()
            {
                EasyThread.stopAllThreads(true);
                Process.GetCurrentProcess().Kill();
            });
            th.Start();
        }

        private void toolStripTextBox2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.loadLastFoldersList();
        }

        
    }

    public class NodeSorter : IComparer
    {
        public int Compare(object x, object y)
        {
            string tx = ((TreeNode)x).Text;
            string ty = ((TreeNode)y).Text;
            if ((tx.Length < 7) && (ty.Length < 7) &&(tx == getOnly(tx) && (ty == getOnly(ty))))
            {
                return int.Parse(tx).CompareTo(int.Parse(ty));
            }
            else
                return tx.CompareTo(ty);

        }

        public string getOnly(string t, string only = "0123456789")
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in t)
                if (only.Contains(c))
                    sb.Append(c);

            return sb.ToString();

        }
    }



}
