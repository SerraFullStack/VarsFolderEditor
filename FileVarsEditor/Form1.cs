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
                    List<string> filesT= getVarsFiles(globalDbPath).ToList();
                    //filesT.Sort((item1, item2) => item1.CompareTo(item2));
                    filesT.Sort(delegate(string item1, string item2) {
                        if (item1 != null && item2 != null)
                        {
                            string[] fName1 = item1.Split(new char[] { '.', '/', '\\' });
                            string[] fName2 = item2.Split(new char[] { '.', '/', '\\' });

                            int c = 0;
                            Parallel.For(c, fName1.Length, delegate (int curr)
                            {
                                if (isNumber(fName1[curr]))
                                    fName1[curr] = int.Parse(fName1[curr]).ToString("000000000000000");
                            });

                            c = 0;
                            Parallel.For(c, fName2.Length, delegate (int curr)
                            {
                                if (isNumber(fName2[curr]))
                                    fName2[curr] = int.Parse(fName2[curr]).ToString("000000000000000");
                            });


                            string compFName1 = "";
                            foreach (var curr in fName1)
                                compFName1 += curr + ".";

                            string compFName2 = "";
                            foreach (var curr in fName2)
                                compFName2 += curr + ".";

                            return compFName1.CompareTo(compFName2);
                        }
                        else
                            return 0;


                    });
                    string[] files = filesT.ToArray();


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

                        //var att = Path.GetFileName(attFname);
                        var att = attFname.Substring(globalDbPath.Length + 1);

                        if (att.ToLower().Contains("time")) ;
                        char sep = '.';
                        if (att.Contains("\\"))
                            sep = '\\';
                        else if (att.Contains("/"))
                            sep = '/';

                        string[] names = att.Split(sep);
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
                            completeName += sep;
                            //Application.DoEvents();
                        }
                    }

                    /*ivk(delegate ()
                    {
                        treeView1.TreeViewNodeSorter = new NodeSorter();
                        treeView1.Sort();
                    });*/
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
            if (editingNode != e.Node)
                checkBox1.Checked = false;
            else
                checkBox1.Checked = true;

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
                string fName = "";
                ivk(delegate () { fName = node.Name; });

                List<string> childs = new List<string>();
                childs.Add(fName);
                Directory.GetFiles(Path.GetDirectoryName(fName), Path.GetFileName(fName) + "*").ToString();

                int max = 1;
                while (childs.Count > 0)
                {
                    if (File.Exists(childs.Last()))
                    {
                        File.Delete(childs.Last());
                        childs.RemoveAt(childs.Count - 1);
                    }
                    else if (Directory.Exists(childs.Last()))
                    {
                        var tempFiles = Directory.GetFiles(childs.Last());
                        var tempFolders = Directory.GetDirectories(childs.Last());

                        if ((tempFiles.Length == 0) && (tempFolders.Length == 0))
                        {
                            Directory.Delete(childs.Last(), true);
                            childs.RemoveAt(childs.Count-1);
                        }
                        else
                        {
                            childs.AddRange(tempFiles);
                            childs.AddRange(tempFolders);

                            max += tempFiles.Length + tempFolders.Length;
                        }
                    }
                    else
                    {
                        childs.RemoveAt(childs.Count - 1);

                    }

                    ivk(delegate ()
                    {
                        progressBar1.Maximum = max;
                        progressBar1.Value = max - childs.Count;
                    });
                }
                

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
                            if (att > max)
                                att = max;
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

        private string[] getVarsFiles(string path)
        {
            List<string> result = new List<string>();
            result.AddRange(Directory.GetFiles(path));

            var folders = Directory.GetDirectories(path);
            Parallel.ForEach(folders, delegate(string currFolder) 
            {
                string[] fFiles = getVarsFiles(currFolder);
                lock(result)
                {
                    result.AddRange(fFiles);
                }
            });

            for (int cont = result.Count-1; cont >= 0; cont--)
            {
                if (result[cont] == null)
                    result.RemoveAt(cont);
            }

            return result.ToArray();
        }

        public bool isNumber(string n)
        {
            int cont = 0;
            int numbers = 0;
            while (cont < n.Length)
            {
                if (!("0123456789".Contains(n[cont])))
                    return false;
                else if (cont == 10)
                    return false;
                else numbers++;
                cont++;
            }

            return numbers > 0;

        }

        DateTime lastCurrentFileModification = DateTime.MinValue;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if ((checkBox1.Checked) && (editingNode != null))
            {
                DateTime fDateTime = File.GetLastWriteTime(editingNode.Name);
                if (fDateTime != lastCurrentFileModification)
                {
                    editNode(editingNode);
                    lastCurrentFileModification = fDateTime;
                }
            }
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
