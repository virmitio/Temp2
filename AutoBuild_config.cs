using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using CoApp.Toolkit.Collections;

namespace AutoBuild
{
    [XmlRoot(ElementName = "AutoBuild_config", Namespace = "http://coapp.org/automation/build")]
    class AutoBuild_config
    {
        //XML Serialization methods
        public string ToXML()
        {
            XmlSerializer S = new XmlSerializer(typeof(AutoBuild_config));
            StringWriter TW = new StringWriter();
            S.Serialize(TW, this);
            return TW.ToString();
        }
        public static string ToXML(ProjectData obj)
        {
            XmlSerializer S = new XmlSerializer(typeof(AutoBuild_config));
            StringWriter TW = new StringWriter();
            S.Serialize(TW, obj);
            return TW.ToString();
        }
        public static AutoBuild_config FromXML(string XMLinput)
        {
            XmlSerializer S = new XmlSerializer(typeof(AutoBuild_config));
            StringReader SR = new StringReader(XMLinput);
            return (AutoBuild_config)S.Deserialize(SR);
        }
        public static AutoBuild_config FromXML(Stream XMLinput)
        {
            XmlSerializer S = new XmlSerializer(typeof(AutoBuild_config));
            return (AutoBuild_config)S.Deserialize(XMLinput);
        }

        //Actual class data
        [XmlElement]
        public bool DefaultCleanRepo { get; private set; }

        [XmlElement]
        public string Name { get; private set; }

        [XmlArray(IsNullable = false)]
        public EasyDictionary<string,VersionControl> VCSList { get; private set; }

        [XmlArray(IsNullable = false)]
        public List<string> DefaultCommands { get; private set; }


        //Default constructor.  Always good to have one of these.
        public AutoBuild_config()
        {
            Enabled = false;
            KeepCleanRepo = true;
            RepoURL = String.Empty;
            WatchRefs = new List<string>();
            BuildCheckouts = new List<CheckoutObject>();
            Commands = new List<Command>();
        }

        //A copy constructor, because I'm always annoyed when I can't find one.
        public AutoBuild_config(AutoBuild_config source)
        {
            Enabled = source.Enabled;
            KeepCleanRepo = source.KeepCleanRepo;
            RepoURL = source.RepoURL;
            WatchRefs = source.WatchRefs;
            BuildCheckouts = source.BuildCheckouts;
            Commands = source.Commands;
        }

        //And a stream constructor in case I ever feel I need it.
        public AutoBuild_config(Stream XMLinput) : this(FromXML(XMLinput))
        {}

    }
}
