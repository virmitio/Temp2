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
    }
}
