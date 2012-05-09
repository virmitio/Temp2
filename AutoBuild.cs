/**
  *    Copyright 2012 Tim Rogers
  *
  *   Licensed under the Apache License, Version 2.0 (the "License");
  *   you may not use this file except in compliance with the License.
  *   You may obtain a copy of the License at
  *
  *       http://www.apache.org/licenses/LICENSE-2.0
  *
  *   Unless required by applicable law or agreed to in writing, software
  *   distributed under the License is distributed on an "AS IS" BASIS,
  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  *   See the License for the specific language governing permissions and
  *   limitations under the License.
  *
  */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using CoApp.Toolkit.Collections;
using Microsoft.Win32;

namespace AutoBuild
{
    internal enum Errors
    {
        NoError = 0,
        NoCommand = -10,

    }


    class AutoBuild : ServiceBase
    {
        public static AutoBuild_config MasterConfig { get; private set; }
        public static XDictionary<string, ProjectData> Projects { get; private set; }
        private static Queue<string> WaitQueue;
        private static List<string> Running;
        private static int CurrentJobs;
        public static List<Daemon> Daemons;

        public AutoBuild()
        {
            Daemons = new List<Daemon>();
            WaitQueue = new Queue<string>();
            Running = new List<string>();
            Projects = new XDictionary<string, ProjectData>();
            MasterConfig = new AutoBuild_config();
            ServiceName = "AutoBuild";
            EventLog.Log = "Application";
            EventLog.Source = ServiceName;
            // Events to enable
            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = true;
            CanStop = true;
        }

        static void Main()
        {
            AutoBuild Manager = new AutoBuild();
            Manager.OnStart(new string[0]);
            ConsoleKeyInfo c = new ConsoleKeyInfo(' ',ConsoleKey.Spacebar,false,false,false);
            while (!(c.Key.Equals(ConsoleKey.Escape)))
            {
                Console.Out.Write('.');
                System.Threading.Thread.Sleep(1000);
                if (Console.KeyAvailable)
                    c = Console.ReadKey(true);
            }
            Manager.OnStop();
            

            ////uncomment this for release...
            //ServiceBase.Run(new AutoBuild());
        }

        #region Standard Service Control Methods

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// OnStart(): Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            // Always double-check that we have an actual thread to work with...
            InitWorld();
            LoadQueue();
        }

        /// <summary>
        /// OnStop(): Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            // Start by halting any further builds from starting
            CurrentJobs += 10000;

            // We should halt any daemons we have running...
            foreach (var daemon in Daemons)
            {
                daemon.Stop();
            }

            // Save all current configuration info.
            SaveConfig();
            foreach (var proj in Projects.Keys)
            {
                SaveProject(proj);
            }

            //Dump the current build queue to a pending list.
            SaveQueue();

            base.OnStop();
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            // Presently not implemented.
            // This is disabled above in AutoBuild().
        }

        /// <summary>
        /// OnContinue(): Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            // Presently not implemented.
            // This is disabled above in AutoBuild().
        }

        protected override void OnShutdown()
        {
            OnStop();
            base.OnShutdown();
        }

        #endregion


        protected void InitWorld()
        {
            // Load the master config
            LoadConfig();

            // Find and load all projects
            DirectoryInfo ProjectRoot = new DirectoryInfo(MasterConfig.ProjectRoot);
            if (Directory.Exists(ProjectRoot.ToString()))
            {
                DirectoryInfo[] children = ProjectRoot.GetDirectories();
                foreach (DirectoryInfo child in children)
                {
                    LoadProject(child.Name);
                }
            }
            //-initialize listeners
            foreach (string name in Projects.Keys)
            {
                InitProject(name);
            }

            // Sometime, I need to make this a switchable option.
            // I also need to provide a better plugin mechanism for new listeners.
            if (MasterConfig.UseGithubListener)
            {
                ListenAgent agent = new ListenAgent();
                agent.Logger = (message => WriteEvent(message, EventLogEntryType.Warning, 0, 1));
                if (agent.Start())
                {
                    Daemons.Add(agent);
                }
                else
                {
                    agent.Stop();
                    WriteEvent("ListenAgent failed to start properly.", EventLogEntryType.Error, 0, 0);
                }
            }

            ////-start timers as appropriate
        }


        protected void MasterChanged(AutoBuild_config config)
        {
            SaveConfig();
        }

        protected void ProjectChanged(string project)
        {
            SaveProject(project);
        }


        private static void LoadQueue()
        {
            var regKey = Registry.LocalMachine.CreateSubKey(@"Software\CoApp\AutoBuild Service") ??
             Registry.LocalMachine.OpenSubKey(@"Software\CoApp\AutoBuild Service");
            if (regKey == null)
                throw new Exception("Unable to load registry key.");
            string configfile = (string)(regKey.GetValue("ConfigFile", null));
            string path = Path.GetDirectoryName(configfile);
            if (path == null)
                return;
            path = Path.Combine(path, "PreviouslyQueued.txt");
            
            if (!File.Exists(path)) 
                return;

            string[] queue = File.ReadAllLines(path);
            foreach (var s in queue)
            {
                try
                {
                    Trigger(s);
                }
                catch (Exception e)
                {
                }
            }
            // Clean up the file now that we've read it.
            File.Delete(path);
        }

        private static void SaveQueue()
        {
            var regKey = Registry.LocalMachine.CreateSubKey(@"Software\CoApp\AutoBuild Service") ??
             Registry.LocalMachine.OpenSubKey(@"Software\CoApp\AutoBuild Service");
            if (regKey == null)
                throw new Exception("Unable to load registry key.");
            string configfile = (string)(regKey.GetValue("ConfigFile", null));
            string path = Path.GetDirectoryName(configfile);
            if (path == null)
                return;
            StringBuilder SB = new StringBuilder();
            while (WaitQueue.Count > 0)
            {
                SB.AppendLine(WaitQueue.Dequeue());
            }
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "PreviouslyQueued.txt"), SB.ToString());
        }

        /// <summary>
        /// Loads the master config file from disk.
        /// This will first check for a registry key to locate the config.xml.
        /// If the file cannot be opened or cannot be found, a default config will be loaded
        /// </summary>
        /// <returns>True if a config file was successfully loaded.  False if a default config had to be generated.</returns>
        public bool LoadConfig()
        {
            try
            {
                var regKey = Registry.LocalMachine.CreateSubKey(@"Software\CoApp\AutoBuild Service") ??
                             Registry.LocalMachine.OpenSubKey(@"Software\CoApp\AutoBuild Service");
                if (regKey == null)
                    throw new Exception("Unable to load registry key.");
                string configfile = (string)(regKey.GetValue("ConfigFile", null));
                if (configfile == null)
                {
                    configfile = @"C:\AutoBuild\config.xml";
                    regKey.SetValue("ConfigFile", configfile);
                }
                MasterConfig = AutoBuild_config.FromXML(File.ReadAllText(configfile));
                MasterConfig.Changed += MasterChanged;
                return true;
            }
            catch(Exception e)
            {
                WriteEvent("Unable to load master config:\n"+e.Message+"\n\nDefault config loaded.", EventLogEntryType.Error, 0, 0);
                MasterConfig = new AutoBuild_config();
                MasterConfig.Changed += MasterChanged; 
                return false;
            }

        }

        /// <summary>
        /// Loads a project configuration from disk.
        /// </summary>
        /// <param name="projectName">The name of the project to load.</param>
        /// <param name="overwrite">If true, will reload the project config data even if the project already has a configuration loaded.  (False by default)</param>
        /// <returns>True if the project was loaded successfully.  False otherwise.</returns>
        public bool LoadProject(string projectName, bool overwrite = false)
        {
            if (Projects.ContainsKey(projectName) && !overwrite)
                return false;

            try
            {
                if (projectName == null)
                    throw new ArgumentException("ProjectName cannot be null.");
                if (!Projects.ContainsKey(projectName))
                    throw new ArgumentException("Project not found: " + projectName);

                string file = Path.Combine(MasterConfig.ProjectRoot, projectName, "config.xml");
                Projects[projectName] = ProjectData.FromXML(File.ReadAllText(file));
                Projects[projectName].SetName(projectName);
                Projects[projectName].Changed2 += ProjectChanged;
                return true;
            }
            catch (Exception e)
            {
                WriteEvent("Unable to load project config ("+projectName+"):\n"+e.Message,EventLogEntryType.Error,0,0);
                return false;
            }
        }

        /// <summary>
        /// Saves the master configuration data to disk.
        /// </summary>
        /// <returns>True if saved successfully.  False otherwise.</returns>
        public bool SaveConfig()
        {
            try
            {
                var regKey = Registry.LocalMachine.CreateSubKey(@"Software\CoApp\AutoBuild Service") ??
                             Registry.LocalMachine.OpenSubKey(@"Software\CoApp\AutoBuild Service");
                if (regKey == null)
                    throw new Exception("Unable to load registry key.");
                string configfile = (string)(regKey.GetValue("ConfigFile", null));
                if (configfile == null)
                {
                    configfile = @"C:\AutoBuild\config.xml";
                    regKey.SetValue("ConfigFile", configfile);
                }
                if (!Directory.Exists(Path.GetDirectoryName(configfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(configfile));
                File.WriteAllText(configfile, MasterConfig.ToXML());
                return true;
            }
            catch (Exception e)
            {
                WriteEvent("Unable to save master config:\n" + e.Message, EventLogEntryType.Error, 0, 0);
                return false;
            }

        }

        /// <summary>
        /// Saves a project config to disk.
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns>True if saved successfully.  False otherwise.</returns>
        public bool SaveProject(string projectName)
        {
            try
            {
                if (projectName == null)
                    throw new ArgumentException("ProjectName cannot be null.");
                if (!Projects.ContainsKey(projectName))
                    throw new ArgumentException("Project not found: " + projectName);

                string file = Path.Combine(MasterConfig.ProjectRoot, projectName, "config.xml");
                if (!Directory.Exists(Path.GetDirectoryName(file)))
                    Directory.CreateDirectory(Path.GetDirectoryName(file));

                File.WriteAllText(file, Projects[projectName].ToXML());
                return true;
            }
            catch (Exception e)
            {
                WriteEvent("Unable to save project config (" + projectName + "):\n" + e.Message, EventLogEntryType.Error, 0, 0);
                return false;
            }

        }

        protected void WriteEvent(string Message, EventLogEntryType EventType, int ID, short Category)
        {
            EventLog.WriteEntry(Message, EventType, ID, Category);
        }

        public static void AddProject(string projectName, ProjectData project)
        {
            if (Projects.ContainsKey(projectName))
                throw new ArgumentException("A project with this name already exists: " + projectName);
            Projects.Add(projectName, project);
            InitProject(projectName);
        }

        public static void InitProject(string projectName)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: " + projectName);
            foreach (var trigger in Projects[projectName].BuildTriggers)
                trigger.Init();
        }

        private static void StartBuild(string projectName)
        {
            BuildStatus build = new BuildStatus();
            if (PreBuildActions(projectName, build) == 0)
                if (Build(projectName, build) == 0)
                    if (PostBuildActions(projectName, build) == 0)
                        build.ChangeResult("Success");
                    else
                        build.ChangeResult("Warning");
                else
                    build.ChangeResult("Failed");
            else
                build.ChangeResult("Error");
            Projects[projectName].GetHistory().Append(build);
        }

        public static void Trigger(string projectName)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: " + projectName);

            if (!(WaitQueue.Contains(projectName) || Running.Contains(projectName)) || Projects[projectName].AllowConcurrentBuilds)
                WaitQueue.Enqueue(projectName);
            Task.Factory.StartNew(ProcessQueue);
        }

        public static void ProcessQueue()
        {
            if (WaitQueue.Count > 0)
            {
                while (CurrentJobs < MasterConfig.MaxJobs)
                {
                    CurrentJobs += 1;
                    string proj = WaitQueue.Dequeue();
                    Task.Factory.StartNew(() =>
                                              {
                                                  Running.Add(proj);
                                                  StartBuild(proj);
                                              }, TaskCreationOptions.AttachedToParent).ContinueWith(
                                  antecedent =>
                                              {
                                                  Running.Remove(proj);
                                                  CurrentJobs -= 1;
                                                  Task.Factory.StartNew(ProcessQueue);
                                              });
                }
            }
        }

        private static int doActions(string projectName, IEnumerable<string> commands, BuildStatus status = null)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: " + projectName);

            status = status ?? new BuildStatus();
            ProjectData proj = Projects[projectName];
            ProcessUtility _cmdexe = new ProcessUtility("cmd.exe");
            // Redirect stdout and stderr to the same output
            StringBuilder std = new StringBuilder();
            _cmdexe.ResetStdOut(std);
            _cmdexe.ResetStdErr(std);

            foreach (string command in commands)
            {
                status.Append("AutoBuild - Begin command:  " + command);

                Command tmp;
                if (proj.Commands.ContainsKey(command))
                {
                    tmp = proj.Commands[command];
                }
                else if (MasterConfig.Commands.ContainsKey(command))
                {
                    tmp = MasterConfig.Commands[command];
                }
                else
                {
                    // Can't locate the specified command.  Bail with error.
                    status.Append("AutoBuild Error:  Unable to locate command script: " + command);
                    return (int)Errors.NoCommand;
                }

                int retVal = tmp.Run(projectName, _cmdexe, new object[0]);
                status.Append(_cmdexe.StandardOut);
                if (retVal != 0)
                    return retVal;
            }

            return 0;
        }

        private static int PreBuildActions(string projectName, BuildStatus status = null)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: " + projectName);

            return doActions(projectName, Projects[projectName].PreBuild, status);
        }

        private static int PostBuildActions(string projectName, BuildStatus status = null)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: " + projectName);

            return doActions(projectName, Projects[projectName].PostBuild, status);
        }

        private static int Build(string projectName, BuildStatus status = null)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: " + projectName);

            return doActions(projectName, Projects[projectName].Build, status);
        }


    }
}
