using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CoApp.Toolkit.Utility;

namespace AutoBuild
{
    [XmlRoot(ElementName = "CommandScript", Namespace = "http://coapp.org/automation/build")]
    public class CommandScript : XmlObject
    {
        [XmlAttribute]
        public string Name;
        [XmlArray(IsNullable = false)]
        public List<string> Commands;

        public CommandScript()
        {
            Name = String.Empty;
            Commands = new List<string>();
        }
        public CommandScript(string name)
        {
            Name = name;
            Commands = new List<string>();
        }
        public CommandScript(string name, IEnumerable<string> lines)
        {
            Name = name;
            Commands = new List<string>(lines);
        }

        public int Run(string path)
        {
            ProcessUtility _cmdexe = new ProcessUtility("cmd.exe");
            return Run(_cmdexe, path);
        }

        public int Run(ProcessUtility exe, string path)
        {
            // Reset my working directory.
            Environment.CurrentDirectory = path;
                
            string tmpFile = Path.GetTempFileName();
            File.Move(tmpFile, tmpFile+".bat");
            tmpFile += ".bat";
            FileStream file = new FileStream(tmpFile,FileMode.Open);
            StreamWriter FS = new StreamWriter(file);

            foreach (string command in Commands)
            {
                FS.WriteLine(command);
            }

            FS.Close();
            return exe.Exec(@"/c """ + tmpFile + @"""");
        }

    }
}
