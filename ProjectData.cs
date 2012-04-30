using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace AutoBuild
{
    [XmlRoot(ElementName = "ProjectData", Namespace = "http://coapp.org/automation/build")]
    public class ProjectData : XmlObject
    {
        //XML Serialization methods
        public string ToXML()
        {
            XmlSerializer S = new XmlSerializer(typeof(ProjectData));
            StringWriter TW = new StringWriter();
            S.Serialize(TW, this);
            return TW.ToString();
        }
        public static string ToXML(ProjectData obj)
        {
            XmlSerializer S = new XmlSerializer(typeof(ProjectData));
            StringWriter TW = new StringWriter();
            S.Serialize(TW, obj);
            return TW.ToString();
        }
        public static ProjectData FromXML(string XMLinput)
        {
            XmlSerializer S = new XmlSerializer(typeof(ProjectData));
            StringReader SR = new StringReader(XMLinput);
            return (ProjectData)S.Deserialize(SR);
        }
        public static ProjectData FromXML(Stream XMLinput)
        {
            XmlSerializer S = new XmlSerializer(typeof(ProjectData));
            return (ProjectData)S.Deserialize(XMLinput);
        }

        //Inner classes
        [XmlRoot(ElementName = "CheckoutInfo", Namespace = "http://coapp.org/automation/build")]
        public class CheckoutInfo : XmlObject
        {
            [XmlAttribute]
            public string Reference;
            [XmlAttribute]
            public string BuildCmd;
            [XmlAttribute]
            public string ArchiveCmd;

            public CheckoutInfo()
            {
                Reference = String.Empty;
                BuildCmd = String.Empty;
                ArchiveCmd = String.Empty;
            }
        }
        
        //Actual class data
        [XmlAttribute]
        public bool Enabled;

        [XmlAttribute]
        public bool KeepCleanRepo;
        
        [XmlAttribute]
        public string RepoURL;

        [XmlArray(IsNullable = false)]
        public List<string> WatchRefs;

        [XmlArray(IsNullable = false)]
        public List<CheckoutInfo> BuildCheckouts;

        [XmlArray(IsNullable = false)]
        public List<CommandScript> Commands;

        //Default constructor.  Always good to have one of these.
        public ProjectData()
        {
            Enabled = false;
            KeepCleanRepo = true;
            RepoURL = String.Empty;
            WatchRefs = new List<string>();
            BuildCheckouts = new List<CheckoutInfo>();
            Commands = new List<CommandScript>();
        }

        //A copy constructor, because I'm always annoyed when I can't find one.
        public ProjectData(ProjectData source)
        {
            Enabled = source.Enabled;
            KeepCleanRepo = source.KeepCleanRepo;
            RepoURL = source.RepoURL;
            WatchRefs = source.WatchRefs;
            BuildCheckouts = source.BuildCheckouts;
            Commands = source.Commands;
        }

        //And a stream constructor in case I ever feel I need it.
        public ProjectData(Stream XMLinput) : this(FromXML(XMLinput))
        {}

    }
}
