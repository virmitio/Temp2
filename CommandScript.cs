using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace AutoBuild
{
    [XmlRoot(ElementName = "CommandScript", Namespace = "http://coapp.org/automation/build")]
    public class CommandScript : XmlObject
    {
//        [XmlAttribute]
//        public string Name;
        [XmlArray(IsNullable = false)]
        public List<string> Commands;

        public CommandScript()
        {
//            Name = String.Empty;
            Commands = new List<string>();
        }
        public CommandScript(string name)
        {
//            Name = name;
            Commands = new List<string>();
        }
        public CommandScript(string name, IEnumerable<string> lines)
        {
//            Name = name;
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

        public string Flatten()
        {
            StringBuilder Out = new StringBuilder();
            foreach (string s in Commands)
            {
                Out.AppendLine(s);
            }
            return Out.ToString();
        }

/*
        /// <summary>
        /// This only compares against the name of the CommandScript.  For a command-level comparison, use Compare(object).
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            if (!(obj is CommandScript || obj is string))
                return false;
            string tmp;
            if (obj is CommandScript)
                tmp = ((CommandScript) obj).Name;
            else
                tmp = (string) obj;
            return Name.Equals(tmp, StringComparison.CurrentCultureIgnoreCase);
        }
*/

        public bool Compare(object obj)
        {
            if (!(obj is CommandScript || obj is string || obj is List<string>))
                return false;
            string tmp;
            if (obj is CommandScript)
                tmp = ((CommandScript)obj).Flatten();
            else if (obj is List<string>)
            {
                StringBuilder SB = new StringBuilder();
                foreach (string s in (List<string>)obj)
                    SB.AppendLine(s);
                tmp = SB.ToString();
            }
            else
                tmp = (string)obj;
            return tmp.Equals(Flatten(),StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
