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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using Microsoft.Win32;

namespace AutoBuild
{
    class AutoBuild : ServiceBase
    {
        private Thread MasterThread;
        private AutoBuild_config MasterConfig;

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

    }
}
