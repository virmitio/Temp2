using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using CoApp.Toolkit.Collections;

namespace AutoBuild
{
    public delegate void MasterConfigChangeHandler(AutoBuild_config sender);

    [XmlRoot(ElementName = "AutoBuild_config", Namespace = "http://coapp.org/automation/build")]
    public class AutoBuild_config
    {
        private const string DEFAULTROOT = @"C:\AutoBuild\Packages";

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
        public event MasterConfigChangeHandler Changed;

        [XmlElement]
        public bool DefaultCleanRepo
        {
            get { return _DefaultCleanRepo; }
            set
            {
                ChangedEvent();
                _DefaultCleanRepo = value;
            }
        }
        private bool _DefaultCleanRepo;

        [XmlElement]
        public bool UseGithubListener
        {
            get { return _UseGithubListener; }
            set
            {
                ChangedEvent();
                _UseGithubListener = value;
            }
        }
        private bool _UseGithubListener;

        [XmlElement]
        public string ProjectRoot
        {
            get { return _ProjectRoot; }
            set
            {
                ChangedEvent();
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
                ChangedEvent();
                _MaxJobs = value;
            }
        }
        private int _MaxJobs;
        
//        [XmlArray(IsNullable = false)]
        [XmlElement]
        public XDictionary<string, VersionControl> VersionControlList;

//        [XmlArray(IsNullable = false)]
        [XmlElement]
        public XDictionary<string, List<string>> DefaultCommands;

//        [XmlArray(IsNullable = false)]
        [XmlElement]
        public XDictionary<string, CommandScript> Commands;



        private void ChangedEvent()
        {
            if (Changed != null)
                Changed(this);
        }


        private void DefaultCommandsChanged(IDictionary<string, List<string>> dict)
        {
            ChangedEvent();
        }
        private void VCSChanged(IDictionary<string, VersionControl> dict)
        {
            ChangedEvent();
        }
        private void CommandsChanged(IDictionary<string, CommandScript> dict)
        {
            ChangedEvent();
        }






        //Default constructor.  Always good to have one of these.
        public AutoBuild_config()
        {
            _DefaultCleanRepo = true;
            _UseGithubListener = true;
            _ProjectRoot = DEFAULTROOT;
            _MaxJobs = 4;
            VersionControlList = new XDictionary<string, VersionControl>();
            DefaultCommands = new XDictionary<string, List<string>>();
            Commands = new XDictionary<string, CommandScript>();

            VersionControlList.Changed += VCSChanged;
            DefaultCommands.Changed += DefaultCommandsChanged;
            Commands.Changed += CommandsChanged;
        }

        //A copy constructor, because I'm always annoyed when I can't find one.
        public AutoBuild_config(AutoBuild_config source)
        {
            _DefaultCleanRepo = source.DefaultCleanRepo;
            _UseGithubListener = source.UseGithubListener;
            _ProjectRoot = source.ProjectRoot;
            _MaxJobs = source.MaxJobs;
            VersionControlList = new XDictionary<string, VersionControl>(source.VersionControlList);
            DefaultCommands = new XDictionary<string, List<string>>(source.DefaultCommands);
            Commands = new XDictionary<string, CommandScript>(source.Commands);

            VersionControlList.Changed += VCSChanged;
            DefaultCommands.Changed += DefaultCommandsChanged;
            Commands.Changed += CommandsChanged;
        }

        //And a stream constructor in case I ever feel I need it.
        public AutoBuild_config(Stream XMLinput) : this(FromXML(XMLinput))
        {
            VersionControlList.Changed += VCSChanged;
            DefaultCommands.Changed += DefaultCommandsChanged;
            Commands.Changed += CommandsChanged;
        }

    }
}
