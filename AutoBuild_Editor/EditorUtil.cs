using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoBuilder
{
    internal class NodeData
    {
        public Type type;
        public object data;

        public NodeData(Type T, object Data)
        {
            type = T;
            data = Data;
        }
    }

    internal static class NodeExtensions
    {
        internal static void AddChild(this TreeNode root, string text, object nodeTag)
        {
            TreeNode node = new TreeNode(text);
            node.Tag = nodeTag;
            root.Nodes.Add(node);
        }

        internal static void AddChild(this TreeNode root, TreeNode child)
        {
            root.Nodes.Add(child);
        }

        internal static TreeNode Root(this TreeNode child)
        {
            return child.Parent == null ? child : child.Parent.Root();
        }
    }

    internal static class EditorUtil
    {
        public static bool? ConfirmPrompt(string message)
        {
            var ans = MessageBox.Show(message, "Please confirm...", MessageBoxButtons.YesNoCancel);
            if (ans == DialogResult.Yes)
            {
                return true;
            }
            if (ans == DialogResult.No)
            {
                return false;
            }
            return null;
        }

        public static void FillObject(object o)
        {
            foreach (var info in o.GetType().GetFields(Editor.DefaultBF))
            {
                if (info.GetValue(o) == null)
                {
                    if (info.FieldType.IsPrimitive)
                        info.SetValue(o, info.FieldType.TypeInitializer.Invoke(null));
                    else
                    {
                        var newO = Activator.CreateInstance(info.FieldType, true);
                        info.SetValue(o, newO);
                        FillObject(info.GetValue(newO));
                    }
                }
            }

        }
    }

    internal static class PromptDialog
    {
        public static string Show(string prompt, string caption = "Prompt", string init = "")
        {
            Form win = new Form();
            win.Width = 400;
            win.Height = 140;
            win.Text = caption;
            Label label = new Label() {Left = 50, Top = 20, Text = prompt};
            TextBox text = new TextBox() {Left = 50, Top = 50, Width = 350, AcceptsReturn = false};
            Button OK = new Button() {Text = "Ok", Left = 50, Width = 100, Top = 70};
            Button Cancel = new Button() { Text = "Cancel", Left = 250, Width = 100, Top = 70 };
            OK.Click += (sender, e) =>
                            {
                                win.DialogResult = DialogResult.OK;
                                win.Close();
                            };
            Cancel.Click += (sender, e) =>
                                {
                                    win.DialogResult = DialogResult.Cancel;
                                    win.Close();
                                };
            win.AcceptButton = OK;
            win.Controls.Add(OK);
            win.Controls.Add(Cancel);
            win.Controls.Add(label);
            win.Controls.Add(text);
            text.Select();
            return win.ShowDialog() == DialogResult.OK ? text.Text : null;
        }
    }
}
