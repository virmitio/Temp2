using System;
using System.Collections.ObjectModel;
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
        private bool Changed;

        [XmlElement]
        public bool DefaultCleanRepo
        {
            get { return _DefaultCleanRepo; }
            set
            {
                Changed = true;
                _DefaultCleanRepo = value;
            }
        }
        private bool _DefaultCleanRepo;

        [XmlElement]
        public string ProjectRoot
        {
            get { return _ProjectRoot; }
            set
            {
                Changed = true;
                _ProjectRoot = value;
            }
        }
        private string _ProjectRoot;

        [XmlElement]
        public int MaxJobs
        {
            get { return _MaxJobs; }
            set
            {
                Changed = true;
                _MaxJobs = value;
            }
        }
        private int _MaxJobs;
        
        [XmlArray(IsNullable = false)]
        public XDictionary<string, VersionControl> VCSList { get; internal set; }

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> DefaultCommands { get; internal set; }

        [XmlArray(IsNullable = false)]
        public XDictionary<string, CommandScript> Commands { get; internal set; }





        void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Changed = true;
        }
        void DictionaryChanged(IDictionary<string, CommandScript> dict)
        {
            Changed = true;
        }






        //Default constructor.  Always good to have one of these.
        public AutoBuild_config()
        {
            Changed = false;



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
