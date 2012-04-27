using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

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
    }
}
