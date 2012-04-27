﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AutoBuild
{
    [XmlRoot(ElementName = "Tool", Namespace = "http://coapp.org/automation/build")]
    public class Tool
    {
        [XmlElement]
        public string Name { get; private set; }
        [XmlElement]
        public string Path { get; private set; }
        [XmlElement]
        public string Switches { get; private set; }

        public Tool(string name, string path, string switches)
        {
            Name = name;
            Path = System.IO.File.Exists(path)?path:
                System.IO.File.Exists(System.IO.Path.GetFullPath(path))?System.IO.Path.GetFullPath(path):
                null;
            Switches = switches;
        }

        /// <summary>
        /// Sets a new path for this tool.
        /// This will ALWAYS set the path, regardless of whether it exists or not.
        /// </summary>
        /// <param name="newPath"></param>
        /// <returns>True - If the new path appears to exist.  False otherwise.</returns>
        public bool SetPath(string newPath)
        {
            Path = newPath;
            return System.IO.File.Exists(newPath) || System.IO.File.Exists(System.IO.Path.GetFullPath(newPath));
        }

        public void SetSwitches(string newSwitches)
        {
            Switches = newSwitches;
        }
    }


}