using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using CoApp.Toolkit.Pipes;

namespace AutoBuilder
{

    public partial class Editor : Form
    {
        private static readonly int initYPos = 6;
        private static readonly int initXPos = 6;
        private static readonly string RootNodeName = "configuration";
        internal static readonly BindingFlags DefaultBF = BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Public |
                  BindingFlags.Instance;

        private int yPos = initYPos;

        public Editor()
        {
            InitializeComponent();
        }

        internal void ClearControls()
        {
            yPos = initYPos;
            Split.Panel2.Controls.Clear();
        }

        internal void AddControl(Control item)
        {
            item.Location = new Point(initXPos, yPos);
            yPos += (item.Size.Height + 6);
            Split.Panel2.Controls.Add(item);
        }

        // This will populate a subtree of nodes with additional nodes for each enumerable 
        //  field/property of the object stored in the current subtree's root [current].
        private static void FillData(TreeNode current)
        {

            var data = current.Tag;
            
            
            // Run once through for fields...
            var fields = data.GetType().GetFields(DefaultBF);
            foreach (var info in fields)
            {
                var each = info.GetValue(data);
                if (each is IEnumerable &&
                    !(each is String))
                {
                    TreeNode node = new TreeNode(info.Name);
                    node.Tag = each;

                    foreach (var item in (IEnumerable)each)
                    {
                        node.AddChild(item.GetType().ToString(), item);
                    }
                    current.AddChild(node);
                }
            }

            // ... and once through for properties.
            var props = data.GetType().GetProperties(DefaultBF);
            foreach (var info in props)
            {
                var each = info.GetValue(data, null);
                if (each is IEnumerable &&
                    !(each is String))
                {
                    TreeNode node = new TreeNode(info.Name);
                    node.Tag = each;

                    foreach (var item in (IEnumerable)each)
                    {
                        node.AddChild(item.GetType().ToString(), item);
                    }
                    current.AddChild(node);
                }
            }

            // repeat for the child objects
            foreach (TreeNode node in current.Nodes)
            {
                foreach (TreeNode subNode in node.Nodes)
                {
                    FillData(subNode);
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ConfigTree.Nodes.Count > 0)
            {
                var ans = EditorUtil.ConfirmPrompt("Save before closing existing work?");
                if (ans == true)
                {
                    saveToolStripMenuItem_Click(sender, e);
                }
                if (ans == null)
                {
                    return;
                }
            }

            ConfigTree.Nodes.Clear();

            string loc = String.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(@"Software\CoApp\AutoBuild Service");
            if (regKey == null)
                loc = @"C:\AutoBuild";
            else
                loc = Path.GetDirectoryName((string) (regKey.GetValue("ConfigFile", @"C:\AutoBuild\config.conf")));

            var OFD = new OpenFileDialog();
            OFD.InitialDirectory = loc;
            OFD.Filter = @"AutoBuild Config files|config.conf|All Files (*.*)|*.*";
            OFD.FilterIndex = 1;
            OFD.RestoreDirectory = true;

            if (OFD.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                if (!File.Exists(OFD.FileName))
                {
                    throw new FileNotFoundException("Unable to open specified file.", OFD.FileName);
                }
                TreeNode root = new TreeNode(RootNodeName);
                root.Tag = OFD.FileName;
                ConfigTree.Nodes.Add(root);

                // open file, produce and attach subnodes to root node for file
                UrlEncodedMessage UEM = new UrlEncodedMessage(File.ReadAllText(OFD.FileName),
                                                              AutoBuild.SerialSeperator, true);

                TreeNode top;

                if (UEM["$T$"].Contains("AutoBuild_config"))
                {
                    var input = UEM.DeserializeTo<AutoBuild_config>();
                    top = new TreeNode(input.GetType().ToString());
                    top.Tag = input;
                }
                else if (UEM["$T$"].Contains("ProjectData"))
                {
                    var input = UEM.DeserializeTo<ProjectData>();
                    top = new TreeNode(input.GetType().ToString());
                    top.Tag = input;
                }
                else
                {
                    var input = UEM.DeserializeTo<Object>();
                    top = new TreeNode(input.GetType().ToString());
                    top.Tag = input;
                }

                root.AddChild(top);
                FillData(top);
            }
            catch (Exception E)
            {
                MessageBox.Show("Unable to open file.\n\n" + E.Message);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                TreeNode root = ConfigTree.Nodes[RootNodeName];
                string filename = (string)(root.Tag);
                NodeData data = (NodeData)(root.FirstNode.Tag);
                File.WriteAllText(filename, data.data.Serialize(AutoBuild.SerialSeperator, true));
            }
            catch (Exception E)
            {
                throw new Exception("Unable to save file.", E);
            }
        }

        private void ConfigTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ClearControls();
            var data = ConfigTree.SelectedNode;

            // Run once through for fields...
            var fields = data.GetType().GetFields(DefaultBF);
            foreach (var info in fields)
            {
                var each = info.GetValue(data);
                if (each is IEnumerable && !(each is String))
                { continue; }

                var C = new GroupBox();
                C.Name = info.Name;
                C.Text = info.Name;
                C.TabStop = false;

                //I haven't found a way to do this as a switch statement yet...
                if (each is bool)
                {
                    Action<bool> change =  (newVal) => info.SetValue(data, newVal);

                    var bTrue = new RadioButton();
                    var bFalse = new RadioButton();

                    bTrue.Name = "bTrue";
                    bTrue.Location = new Point(6, 20);
                    bTrue.TabIndex = 0;
                    bTrue.TabStop = true;
                    bTrue.Text = "True";
                    bTrue.Size = new Size(47, 17);
                    bTrue.CheckedChanged += (o, eArgs) => { if (bTrue.Checked) change(true); };


                    bFalse.Name = "bFalse";
                    bFalse.Location = new Point(60, 20);
                    bFalse.TabIndex = 1;
                    bFalse.TabStop = true;
                    bFalse.Text = "False";
                    bFalse.Size = new Size(50, 17);
                    bFalse.CheckedChanged += (o, eArgs) => { if (bFalse.Checked) change(false); };
                    
                    if ((bool)each)
                    {
                        bTrue.Checked = true;
                        bFalse.Checked = false;
                    }
                    else
                    {
                        bTrue.Checked = false;
                        bFalse.Checked = true;
                    }

                    C.Controls.Add(bTrue);
                    C.Controls.Add(bFalse);
                    C.Size = new Size(120, 42);

                }
                else if (each is int)
                {
                    var spinner = new NumericUpDown();

                    Action<bool> change = (fromText) =>
                                                           {
                                                               int val;
                                                               if (fromText)
                                                               {
                                                                   if (int.TryParse(spinner.Text, out val))
                                                                       info.SetValue(data, val);
                                                               }
                                                               else
                                                               {
                                                                   val = Decimal.ToInt32(spinner.Value);
                                                                   info.SetValue(data, val);
                                                               }
                                                           };
                    spinner.TabIndex = 0;
                    spinner.TabStop = true;
                    spinner.Value = (decimal) each;
                    spinner.Text = ((int)each).ToString();
                    spinner.ValueChanged += (o, eArgs) => change(false);
                    spinner.TextChanged += (o, eArgs) => change(true);
                    spinner.Location = new Point(6, 20);
                    spinner.Size = new Size(60, 20);
                    C.Controls.Add(spinner);
                    C.Size = new Size(74, 45);
                }
                else if (each is __)
                {

                }
                else if (each is __)
                {

                }
                else if (each is __)
                {

                }
                else if (each is __)
                {

                }


                AddControl(C);
            }

            // ... and once through for properties.
            var props = data.GetType().GetProperties(DefaultBF);
            foreach (var info in props)
            {
                var each = info.GetValue(data, null);
                if (each is IEnumerable && !(each is String))
                { continue; }



            }


        }

    }
}
