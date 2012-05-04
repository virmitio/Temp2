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
        private Thread MasterThread;
        public static AutoBuild_config MasterConfig;
        public static EasyDictionary<string, ProjectData> Projects { get; private set; }

        public AutoBuild()
        {
            MasterThread = new Thread(MasterControl);
            ServiceName = "AutoBuild";
            EventLog.Log = "Application";
            EventLog.Source = ServiceName;
            // Events to enable
            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = true;
            CanShutdown = true;
            CanStop = true;
        }

        static void Main()
        {
            /*
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
             */


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
            MasterThread = MasterThread ?? new Thread(MasterControl);
            MasterThread.Start();
        }

        /// <summary>
        /// OnStop(): Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            //I think this may be the wrong answer here, but I don't know yet.
            MasterThread.Abort();
            MasterThread.Join();

            base.OnStop();
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            //do something
            UpdateTimer.Change(0, TimerReset);
            base.OnPause();
        }

        /// <summary>
        /// OnContinue(): Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            UpdateTimer.Change(0, TimerReset);
            base.OnContinue();
        }

        #endregion


        protected void MasterControl()
        {
            //-locate config files
            //-read master config file
            //-initialize listeners
            //-scan projects
            ////-read project config files
            ////-start timers as appropriate
            
            /*
             * I need a thread/task pool to limit the number of projects trying 
             *  to run at once.  I also need a queueing system for that pool and
             *  easy-to-use methods for interacting with it (otherwise I'll forget
             *  how to use it).
             * Listener and timer actions should place requests in the queue when
             *  triggered.  They should also be smart enough to not place 
             *  themselves on the queue multiple times at once.
             */
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

        public static void Trigger(string projectName)
        {
            if (PreBuildActions(projectName) == 0)
                if (Build(projectName) == 0)
                    if (PostBuildActions(projectName) == 0)
                        Record("Success", projectName);
                    else
                        Record("Warning", projectName);
                else
                    Record("Failed", projectName);
            else
                Record("Error", projectName);

        }

        public static int PreBuildActions(string projectName, BuildStatus status = null)
        {
        }

        public static int PostBuildActions(string projectName, BuildStatus status = null)
        {
        }

        public static int Build(string projectName, BuildStatus status = null)
        {
            if (projectName == null)
                throw new ArgumentException("ProjectName cannot be null.");
            if (!Projects.ContainsKey(projectName))
                throw new ArgumentException("Project not found: "+projectName);

            status = status ?? new BuildStatus();
            ProjectData proj = Projects[projectName];
            ProcessUtility _cmdexe = new ProcessUtility("cmd.exe");
            // Redirect stdout and stderr to the same output
            StringBuilder std = new StringBuilder();
            _cmdexe.ResetStdOut(std);
            _cmdexe.ResetStdErr(std);
            
            foreach (string command in proj.Build)
            {
                status.Append("AutoBuild - Begin command:  " + command);

                CommandScript tmp = new CommandScript(command);
                if (proj.Commands.ContainsKey(tmp))
                {
                    tmp = proj.Commands[tmp];
                }
                else if (MasterConfig.Commands.ContainsKey(tmp))
                {
                    tmp = MasterConfig.Commands[tmp];
                }
                else
                {
                    // Can't locate the specified command.  Bail with error.
                    status.Append("AutoBuild Error:  Unable to locate command script: " + command);
                    return (int)Errors.NoCommand;
                }

                int retVal = tmp.Run(_cmdexe, MasterConfig.ProjectRoot + @"\" + projectName);
                status.Append(_cmdexe.StandardOut);
                if (retVal != 0)
                    return retVal;
            }

            return 0;
        }
    }
}
