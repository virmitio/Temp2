using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using System.Xml.Serialization;
using System.Collections.Generic;
using CoApp.Toolkit.Collections;

namespace AutoBuild
{
    public delegate void ProjectChangeHandler(ProjectData sender);
    public delegate void AltProjectChangeHandler(string sender);

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
        private BuildHistory History;
        private string Name;

        public event ProjectChangeHandler Changed;
        public event AltProjectChangeHandler Changed2;

        [XmlElement]
        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                ChangedEvent();
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
                ChangedEvent();
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
                ChangedEvent();
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
                ChangedEvent();
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
                ChangedEvent();
                _VersionControl = value;
            }
        }
        private string _VersionControl;

        [XmlArray(IsNullable = false)] 
        public ObservableCollection<string> WatchRefs;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<CheckoutInfo> BuildCheckouts;

        [XmlElement]
        public XDictionary<string, Command> Commands;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<BuildTrigger> BuildTriggers;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> PreBuild;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> Build;

        [XmlArray(IsNullable = false)]
        public ObservableCollection<string> PostBuild;

        private void ChangedEvent()
        {
            if (Changed != null)
                Changed(this);
            if (Changed2 != null)
                Changed2(Name);
        }

        void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ChangedEvent();
        }
        void DictionaryChanged(IDictionary<string, Command> dict)
        {
            ChangedEvent();
        }

        public BuildHistory GetHistory()
        {
            return History;
        }

        /// <summary>
        /// Sets the internal name of this project.
        /// This is only used as an internal reference for lookup by the service.
        /// </summary>
        /// <param name="newName"></param>
        public void SetName(string newName)
        {
            Name = newName;
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
            Commands = new XDictionary<string, Command>();
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
            WatchRefs = new ObservableCollection<string>(source.WatchRefs);
            BuildCheckouts = new ObservableCollection<CheckoutInfo>(source.BuildCheckouts);
            Commands = new XDictionary<string, Command>(source.Commands);
            Build = new ObservableCollection<string>(source.Build);
            PreBuild = new ObservableCollection<string>(source.PreBuild);
            PostBuild = new ObservableCollection<string>(source.PostBuild);
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
