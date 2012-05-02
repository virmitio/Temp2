using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CoApp.Toolkit.Collections;

namespace AutoBuild
{
    [XmlRoot(ElementName = "Tool", Namespace = "http://coapp.org/automation/build")]
    public class Tool
    {
        [XmlElement]
        public string Name { get; private set; }
        [XmlElement]
        public string Path { get; private set; }
        [XmlArray(IsNullable = false)]
        public string[] Switches { get; private set; }

        public Tool(string name, string path, string[] switches)
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

        public void SetSwitches(string[] newSwitches)
        {
            Switches = newSwitches;
        }
    }

    [XmlRoot(ElementName = "VersionControl", Namespace = "http://coapp.org/automation/build")]
    public class VersionControl
    {
        [XmlAttribute]
        public string Name { get; private set; }

        [XmlElement]
        public Tool Tool { get; private set; }

        [XmlArray(IsNullable = false)]
        public EasyDictionary<string, object> Properties { get; private set; }

        public void SetTool(Tool tool)
        {
            this.Tool = tool;
        }

        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }

        public bool RemoveProperty(string key)
        {
            return Properties.Remove(key);
        }

        public VersionControl(string name, Tool tool = null, IDictionary<string,object> properties = null)
        {
            if (name == null)
                throw new ArgumentNullException("name", "VersionControl.Name cannot be null.");
            Name = name;
            this.Tool = tool;
            Properties = new EasyDictionary<string, object>(properties);
        }

    }

    [XmlRoot(ElementName = "BuildTrigger", Namespace = "http://coapp.org/automation/build")]
    public abstract class BuildTrigger
    {
        [XmlAttribute]
        public string Type { get; protected set; }

        public abstract void Init();
    }

}
