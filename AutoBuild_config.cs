using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace AutoBuild
{
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
        public bool Enabled;
        public bool KeepCleanRepo;
        public string RepoURL;
        public List<string> WatchRefs;
        public List<CheckoutObject> BuildCheckouts;
        public List<Command> Commands;

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
