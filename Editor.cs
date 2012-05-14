using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoBuild
{
    public partial class Editor : Form
    {
        public Editor()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string loc = String.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(@"Software\CoApp\AutoBuild Service");
            if (regKey == null)
                loc = @"C:\AutoBuild";
            else
                loc = Path.GetDirectoryName((string)(regKey.GetValue("ConfigFile", @"C:\AutoBuild\config.conf")));

            var OFD = new OpenFileDialog();
            OFD.InitialDirectory = loc;
            OFD.Filter = "AutoBuild Config files|config.conf|All Files (*.*)|*.*";
            OFD.FilterIndex = 1;
            OFD.RestoreDirectory = true;

            if (OFD.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // new root node, with filename and location attached

                    // open file, produce and attach subnodes to root node for file
                }
                catch (Exception E)
                {
                    MessageBox.Show("Unable to open file.\n\n" + E.Message);

                }
            }

        }

    }
}
