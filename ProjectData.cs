using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using System.Xml.Serialization;
using System.Collections.Generic;
using CoApp.Toolkit.Collections;

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
        private bool Changed;
        private BuildHistory History;

        [XmlElement]
        public bool Enabled
        {
            get { return _Enabled; }
            set { 
                Changed = true;
                _Enabled = value;
            }
        }
        private bool _Enabled;

        [XmlElement]
        public bool KeepCleanRepo
        {
            get { return _KeepCleanRepo; }
            set
            {
                Changed = true;
                _KeepCleanRepo = value;
            }
        }
        private bool _KeepCleanRepo;

        [XmlElement]
        public bool AllowConcurrentBuilds
        {
            get { return _AllowConcurrentBuilds; }
            set
            {
                Changed = true;
                _AllowConcurrentBuilds = value;
            }
        }
        private bool _AllowConcurrentBuilds;


        [XmlElement]
        public string RepoURL
        {
            get { return _RepoURL; }
            set
            {
                Changed = true;
                _RepoURL = value;
            }
        }
        private string _RepoURL;

        [XmlElement] 
        public string VersionControl
        {
            get { return _VersionControl; }
            set
            {
                Changed = true;
                _VersionControl = value;
            }
        }
        private string _VersionControl;

        [XmlArray(IsNullable = false)] 
        public ObservableCollection<string> WatchRefs;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<CheckoutInfo> BuildCheckouts;

        [XmlArray(IsNullable = false)]
        public XDictionary<string, CommandScript> Commands;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<BuildTrigger> BuildTriggers;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> PreBuild;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> Build;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> PostBuild;

        void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Changed = true;
        }
        void DictionaryChanged(IDictionary<string, CommandScript> dict)
        {
            Changed = true;
        }

        public bool HasChanged()
        {
            return Changed;
        }

        public bool ResetChanged()
        {
            bool tmp = Changed;
            Changed = false;
            return tmp;
        }

        public BuildHistory GetHistory()
        {
            return History;
        }

        /// <summary>
        /// Will attempt to load the build history from a file.
        /// If the file cannot be found, the string will be assumed to contain Xml data and an attempt will be made to parse it for history data.
        /// </summary>
        /// <param name="XmlFile"></param>
        /// <returns>True if the History object has changed.</returns>
        public bool LoadHistory(string XmlFile)
        {
            if (File.Exists(XmlFile))
                return History.ImportHistory(new FileStream(XmlFile, FileMode.Open, FileAccess.Read));
            
            //This means we don't see a file by that name.  Maybe it's just an Xml string?
            return History.ImportHistory(XmlFile);
        }
        
        //Default constructor.  Always good to have one of these.
        public ProjectData()
        {
            Enabled = false;
            KeepCleanRepo = true;
            RepoURL = String.Empty;
            WatchRefs = new ObservableCollection<string>();
            WatchRefs.CollectionChanged += CollectionChanged;
            BuildCheckouts = new ObservableCollection<CheckoutInfo>();
            BuildCheckouts.CollectionChanged += CollectionChanged;
            Commands = new XDictionary<string, CommandScript>();
            Commands.Changed += DictionaryChanged;
            Build = new ObservableCollection<string>();
            Build.CollectionChanged += CollectionChanged;
            PreBuild = new ObservableCollection<string>();
            PreBuild.CollectionChanged += CollectionChanged;
            PostBuild = new ObservableCollection<string>();
            PostBuild.CollectionChanged += CollectionChanged;
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
            Build = source.Build;
            PreBuild = source.PreBuild;
            PostBuild = source.PostBuild;
            WatchRefs.CollectionChanged += CollectionChanged;
            BuildCheckouts.CollectionChanged += CollectionChanged;
            Commands.Changed += DictionaryChanged;
            Build.CollectionChanged += CollectionChanged;
            PreBuild.CollectionChanged += CollectionChanged;
            PostBuild.CollectionChanged += CollectionChanged;
        }

        //And a stream constructor in case I ever feel I need it.
        public ProjectData(Stream XMLinput) : this(FromXML(XMLinput))
        {
            WatchRefs.CollectionChanged += CollectionChanged;
            BuildCheckouts.CollectionChanged += CollectionChanged;
            Commands.Changed += DictionaryChanged;
            Build.CollectionChanged += CollectionChanged;
            PreBuild.CollectionChanged += CollectionChanged;
            PostBuild.CollectionChanged += CollectionChanged;
        }

    }
}
