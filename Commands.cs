using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace AutoBuild
{
    [XmlRoot(ElementName = "Command", Namespace = "http://coapp.org/automation/build")]
    public abstract class Command
    {
        public abstract int Run(string project, ProcessUtility console, object[] args);
    }

    [XmlRoot(ElementName = "Checkout", Namespace = "http://coapp.org/automation/build")]
    public class Checkout : Command
    {
        public override int Run(string project, ProcessUtility console, object[] args)
        {
            string root = AutoBuild.MasterConfig.ProjectRoot + @"\" + project;
            var tool = AutoBuild.MasterConfig.VersionControlList[AutoBuild.Projects[project].VersionControl].Tool;

            if (AutoBuild.Projects[project].KeepCleanRepo)
            {
                if (!Directory.Exists(Path.Combine(root, "Clean")))
                    Directory.CreateDirectory(Path.Combine(root, "Clean"));
                if (Directory.Exists(Path.Combine(root, "Clean", ".git")))
                {
                    //already cloned, just need to fetch and merge
                    Environment.CurrentDirectory = Path.Combine(root, "Clean");
                    List<string> argList = new List<string>();
                    argList.Add("/c"); // run then terminate
                    argList.Add(tool.Path);
                    argList.AddRange(tool.Switches);
                    argList.Add("fetch");
                    argList.Add(AutoBuild.Projects[project].RepoURL);
                    foreach (var info in AutoBuild.Projects[project].BuildCheckouts)
                    {
                        List<string> tmpArgs = new List<string>(argList);
                        tmpArgs.Add(info.Reference);
                        int val = console.Exec(tmpArgs.ToArray());
                        if (val != 0)
                            return val;
                    }
                    
                }
                else
                {
                    //need to clone the repo
                    Environment.CurrentDirectory = root;
                }
            }





            if (!Directory.Exists(Path.Combine(root, "Working")))
                Directory.CreateDirectory(Path.Combine(root, "Working"));
            



            return 0;
        }
    }

    [XmlRoot(ElementName = "CommandScript", Namespace = "http://coapp.org/automation/build")]
    public class CommandScript : Command
    {
        [XmlArray(IsNullable = false)]
        public List<string> Commands;

        public CommandScript()
        {
            Commands = new List<string>();
        }
        public CommandScript(string name)
        {
            Commands = new List<string>();
        }
        public CommandScript(string name, IEnumerable<string> lines)
        {
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

        public int Run(object[] Params)
        {
            try
            {
                if (Params.Length == 1)
                    return Run((string) Params[0]);
                if (Params.Length >= 2)
                    return Run((ProcessUtility) Params[0], (string) Params[1]);
                return (int) Errors.NoCommand;
            }
            catch(Exception e)
            {
                return (int)Errors.NoCommand;
            }
        }

        public override int Run(string project, ProcessUtility console, object[] args)
        {
            return Run(console, AutoBuild.MasterConfig.ProjectRoot + @"\" + project);
        }
    }
}
