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
            
            if (data is string || data is int || data is bool)
            {
                return;
            }
            
            // Run once through for fields...
            var fields = data.GetType().GetFields(DefaultBF);
            foreach (var info in fields)
            {
                var each = info.GetValue(data);
                TreeNode node = new TreeNode(info.Name);
                node.Tag = each;

                if (each is IEnumerable &&
                    !(each is String))
                {
                    if (each is IDictionary)
                        foreach (var item in ((IDictionary)each).Keys)
                        {
                            node.AddChild(item.ToString(), ((IDictionary)each)[item]);
                        }
                    else
                        foreach (var item in (IEnumerable)each)
                        {
                            node.AddChild(item.GetType().Name, item);
                        }
                }
                if (each is string || each is int || each is bool)
                    continue;
                current.AddChild(node);
            }

            // ... and once through for properties.
            var props = data.GetType().GetProperties(DefaultBF);
            foreach (var info in props)
            {
                var para = info.GetIndexParameters();
                object[] arr;
                if (para.Length == 0)
                    arr = null;
                else
                {
                    arr = new object[para.Length];
                    for (int p = 0; p < para.Length; p++)
                        arr[p] = null;
                }
                var each = info.GetValue(data, arr);
                TreeNode node = new TreeNode(info.Name);
                node.Tag = each;

                if (each is IEnumerable &&
                    !(each is String))
                {
                    if (each is IDictionary)
                        foreach (var item in ((IDictionary)each).Keys)
                        {
                            node.AddChild(item.ToString(), ((IDictionary)each)[item]);
                        }
                    else
                        foreach (var item in (IEnumerable)each)
                    {
                        node.AddChild(item.GetType().Name, item);
                    }
                }
                if (each is string || each is int || each is bool)
                    continue;
                current.AddChild(node);
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

                if (UEM[".$T$"].Contains("AutoBuild_config"))
                {
                    var input = UEM.DeserializeTo<AutoBuild_config>();
                    top = new TreeNode(input.GetType().Name);
                    top.Tag = input;
                }
                else if (UEM[".$T$"].Contains("ProjectData"))
                {
                    var input = UEM.DeserializeTo<ProjectData>();
                    top = new TreeNode(input.GetType().Name);
                    top.Tag = input;
                }
                else
                {
                    Type type = Type.GetType(UEM[".$T$"]);
                    var obj = Activator.CreateInstance(type, true);
                    var input = UEM.DeserializeTo(obj);
                    top = new TreeNode(input.GetType().Name);
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
                TreeNode root = ConfigTree.SelectedNode.Root();
                string filename = (string)(root.Tag);
                var data = (root.FirstNode.Tag);
                File.WriteAllText(filename, data.Serialize(AutoBuild.SerialSeperator, true));
            }
            catch (Exception E)
            {
                throw new Exception("Unable to save file.", E);
            }
        }

        private void ConfigTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ClearControls();
            var data = ConfigTree.SelectedNode.Tag;



            Action<bool, GroupBox, Action<bool>> boolMaker = (init, parent, func) =>
                                                                  {
                                                                      var bTrue = new RadioButton();
                                                                      var bFalse = new RadioButton();

                                                                      bTrue.Name = "bTrue";
                                                                      bTrue.Location = new Point(6, 20);
                                                                      bTrue.TabIndex = 0;
                                                                      bTrue.TabStop = true;
                                                                      bTrue.Text = "True";
                                                                      bTrue.Size = new Size(47, 17);
                                                                      bTrue.CheckedChanged +=
                                                                          (o, eArgs) =>
                                                                              { if (bTrue.Checked) func(true); };


                                                                      bFalse.Name = "bFalse";
                                                                      bFalse.Location = new Point(60, 20);
                                                                      bFalse.TabIndex = 1;
                                                                      bFalse.TabStop = true;
                                                                      bFalse.Text = "False";
                                                                      bFalse.Size = new Size(50, 17);
                                                                      bFalse.CheckedChanged +=
                                                                          (o, eArgs) =>
                                                                              { if (bFalse.Checked) func(false); };

                                                                      if (init)
                                                                      {
                                                                          bTrue.Checked = true;
                                                                          bFalse.Checked = false;
                                                                      }
                                                                      else
                                                                      {
                                                                          bTrue.Checked = false;
                                                                          bFalse.Checked = true;
                                                                      }

                                                                      parent.Controls.Add(bTrue);
                                                                      parent.Controls.Add(bFalse);
                                                                      parent.Size = new Size(120, 42);

                                                                  };


            Action<int, GroupBox, Action<int>> intMaker = (init, parent, func) =>
                                                               {
                                                                   var spinner = new NumericUpDown();

                                                                   Action<bool> change = fromText =>
                                                                   {
                                                                       int val;
                                                                       if (fromText)
                                                                       {
                                                                           if (int.TryParse(spinner.Text, out val))
                                                                               func(val);
                                                                       }
                                                                       else
                                                                       {
                                                                           val = Decimal.ToInt32(spinner.Value);
                                                                           func(val);
                                                                       }
                                                                   };
                                                                   spinner.TabIndex = 0;
                                                                   spinner.TabStop = true;
                                                                   spinner.Value = init;
                                                                   spinner.Text = init.ToString();
                                                                   spinner.ValueChanged += (o, eArgs) => change(false);
                                                                   spinner.TextChanged += (o, eArgs) => change(true);
                                                                   spinner.Location = new Point(6, 20);
                                                                   spinner.Size = new Size(60, 20);

                                                                   parent.Controls.Add(spinner);
                                                                   parent.Size = new Size(74, 45);
                                                               };


            Action<string , GroupBox, Action<string >> stringMaker = (init, parent, func) =>
                                                              {
                                                                  var text = new TextBox();
                                                                  text.Multiline = true;
                                                                  text.AcceptsReturn = true;
                                                                  text.AcceptsTab = true;
                                                                  text.Text = init;
                                                                  text.TabIndex = 0;
                                                                  text.TabStop = true;
                                                                  text.Size = new Size(180, 40);
                                                                  text.Location = new Point(6, 20);
                                                                  text.TextChanged += (o, eArgs) => func(text.Text);
                                                                  parent.Size = new Size(190, 65);
                                                                  parent.Controls.Add(text);
                                                              };


            if (data is string)
            {
                var C = new GroupBox();
                C.Name = ConfigTree.SelectedNode.Text;
                C.Text = C.Name;
                C.TabStop = false;

                Action<string> setText = T => data = T;
                stringMaker((string) data, C, setText);
                AddControl(C);
            }
            else if (data is int)
            {
                var C = new GroupBox();
                C.Name = ConfigTree.SelectedNode.Text;
                C.Text = C.Name;
                C.TabStop = false;

                Action<int> setInt = input => data = input;
                intMaker((int) data, C, setInt);
                AddControl(C);
            }
            else if (data is bool)
            {
                var C = new GroupBox();
                C.Name = ConfigTree.SelectedNode.Text;
                C.Text = C.Name;
                C.TabStop = false;

                Action<bool> change = newVal => data = newVal;
                boolMaker((bool) data, C, change);
                AddControl(C);
            }
            if (data is bool || data is int || data is string || data is IEnumerable)
                return;
            
            // Run once through for fields...
            var fields = data.GetType().GetFields(DefaultBF);
            foreach (var info in fields)
            {
                var localInfo = info;
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
                    Action<bool> change = newVal => localInfo.SetValue(data, newVal);

                    boolMaker((bool) each, C, change);
                }
                else if (each is int)
                {
                    Action<int> setInt = input => localInfo.SetValue(data, input);

                    intMaker((int) each, C, setInt);
                }
                else if (each is string)
                {
                    Action<string> setText = T => localInfo.SetValue(data, T);

                    stringMaker((string) each, C, setText);
                }
                else
                {
                    continue;
                }

                AddControl(C);
            }

            // ... and once through for properties.
            var props = data.GetType().GetProperties(DefaultBF);
            foreach (var info in props)
            {
                var localInfo = info;
                var each = info.GetValue(data, null);
                if (each is IEnumerable && !(each is String))
                { continue; }

                var C = new GroupBox();
                C.Name = info.Name;
                C.Text = info.Name;
                C.TabStop = false;

                //I haven't found a way to do this as a switch statement yet...
                if (each is bool)
                {
                    Action<bool> change = newVal => localInfo.SetValue(data, newVal, null);

                    boolMaker((bool)each, C, change);
                }
                else if (each is int)
                {
                    Action<int> setInt = input => localInfo.SetValue(data, input, null);

                    intMaker((int)each, C, setInt);
                }
                else if (each is string)
                {
                    Action<string> setText = T => localInfo.SetValue(data, T, null);

                    stringMaker((string)each, C, setText);
                }
                else
                {
                    continue;
                }

                AddControl(C);
            }

        }

        private void ConfigTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ConfigTree.SelectedNode = ConfigTree.GetNodeAt(e.X, e.Y);
                var node = ConfigTree.SelectedNode.Tag;
                
                if (node is IEnumerable && !(node is string))
                    RightClick_NodeMenu.Items["newToolStripMenuItem"].Enabled = true;
                else
                    RightClick_NodeMenu.Items["newToolStripMenuItem"].Enabled = false;

                var parent = ConfigTree.SelectedNode.Parent.Tag;
                if (parent is IEnumerable && !(parent is string))
                    RightClick_NodeMenu.Items["removeToolStripMenuItem"].Enabled = true;
                else
                    RightClick_NodeMenu.Items["removeToolStripMenuItem"].Enabled = false;

                RightClick_NodeMenu.Show(ConfigTree, e.Location);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = ConfigTree.SelectedNode.Tag;

            

            if (node is IDictionary)
            {
                Type[] types = node.GetType().GetGenericArguments();
                object newobj;
                if (types[1] == typeof(String))
                    newobj = String.Empty;
                else
                    newobj = Activator.CreateInstance(types[1], true);

                EditorUtil.FillObject(newobj);

                string keyName = PromptDialog.Show("New key:") ?? String.Empty;
                if (keyName.Length == 0)
                    return;

                var key = Convert.ChangeType(keyName, types[0]);
                ((IDictionary)node).Add(key, newobj);

                /*
                ConfigTree.SelectedNode.Nodes.Clear();
                FillData(ConfigTree.SelectedNode);
                */
                
                TreeNode N = new TreeNode(keyName);
                N.Tag = newobj;
                ConfigTree.SelectedNode.AddChild(N);
                FillData(N);
                
            }
            else
            {
                
                //node should be castable to a list
                Type T = node.GetType().GetGenericArguments()[0];
                object newobj; 
                if (T == typeof(String))
                    newobj = String.Empty;
                else
                    newobj = Activator.CreateInstance(T, true);
                
                EditorUtil.FillObject(newobj);
                
                ((IList)node).Add(newobj);

                ConfigTree.SelectedNode.Nodes.Clear();
                FillData(ConfigTree.SelectedNode);

                /*
                TreeNode N = new TreeNode(T.Name);
                N.Tag = newobj;
                ConfigTree.SelectedNode.AddChild(N);
                FillData(N);
                */
            }

            
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
