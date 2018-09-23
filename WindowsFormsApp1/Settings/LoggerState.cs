using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MultiDeviceAIO
{
    /// <summary>
    /// Class is a reference type!
    /// Avoids need for ref
    /// Ensures app holds only 1 settings instance -> perhaps make singleton!
    /// </summary>
    [Serializable]
    public partial class LoggerState
    {
        /// <summary>
        /// Replaces {KEY} defined by hard coded KEYS mapped to object values.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string MergeObjectToString(object obj, string str)
        {
            Dictionary<string, string> swaps = MergeDictionary(obj);
            //do the map swaps
            foreach (KeyValuePair<string, string> pair in swaps)
            {
                str = str.Replace("{" + pair.Key + "}", pair.Value);
            }

            return str;
        }

        public static Dictionary<string, string> MergeDictionary(object obj)
        {
            Dictionary<string, string> swaps = new Dictionary<string, string>();
            //prepare the map
            PropertyInfo[] properties = obj.GetType().GetProperties();
            foreach (var prop in properties)
            {
                string propName = prop.Name;
                swaps[propName.ToUpper()] = obj.GetType().GetProperty(propName).GetValue(obj, null).ToString();
            }

            return swaps;
        }

        //public static string default_xml = @"<?xml version=""1.0"" encoding=""utf-16""?><SettingData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><testpath>C:\Users\Alva\Desktop</testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>0</mass><load>0</load><shakertype>0</shakertype><paddtype>1</paddtype><n_devices>0</n_devices><n_channels>64</n_channels><duration>5</duration><timer_interval>1000</timer_interval><external_trigger>false</external_trigger><external_clock>false</external_clock><path>C:\Users\Alva\Desktop\default.xml</path><modified>false</modified></SettingData>";

        public string version { get; } = "2.1";       //Logger content version

        //TODO: shouldn't these be null? And test for null in prog. OR doesn't it serialise?
        public string testpath { get; set; } = "";       //Path to test data
        public string temp_filename {get;set;} = "";  //last temp filename (recover)
        public string datafileformat { get; set; } = "{TESTPATH}\\{LOAD}{CLIPSAB}\\M{MASSNUM}_f{FREQUENCY}";
        public int testingmode { get; set; } = 0;

        //Test parameters
        public float frequency { get; set; } = 0;
        public bool clipsOn { get; set; } = false;
        public int mass { get; set; } = 0;
        public double load { get; set; } = 0;
        public int shakertype { get; set; } = 0;
        public int paddtype { get; set; } = 0;
        //filename mods
        public string massnum {
            get { return (mass + 1).ToString();  }
        }
        public string clipsAB
        {
            get { return ((clipsOn) ? "A" : "B") ; }
        }

        //Sampling parameters
        public int n_devices { get; set; } = 0;
        public short n_channels { get; set; } = 64;
        public int duration { get; set; } = 5;
        public int timer_interval
        {
            get
            {
                return (int)Math.Round(1E6 / sample_frequency,0);
            }
            set
            {
                sample_frequency = (short)(1E6/ value);
            }
        }
        public short sample_frequency { get; set; } = 1000;
        public short n_samples                  //number channels scans before stop
        {
            get
            {
                return (short)(duration * sample_frequency);
            }
        }

        public bool external_trigger { get; set; }
        public bool external_clock { get; set; }

        public List<List<int>> accsetup { get; set; } = null;
        public List<List<double>> caldata { get; set; } = null;

        //Internal parameters
        public string settingspath { get; set; } = "";
        public string auto_template { get; set; } = "";
        public bool modified { get; set; } = false;
    }

    public class PersistentLoggerState : FilePersistentState<LoggerState>
    {
        public static PersistentLoggerState ps = null;

        public PersistentLoggerState() : base(new LoggerState()) { }

        public bool Load(string path)
        {
            if (base.Load(path))
            {
                data.settingspath = path;
                data.modified = false;
                return true;
            } else
            {
                return false;
            }
        }

        public bool Save(string path)
        {
            data.settingspath = path;
            data.modified = false;
            return base.Save(path);
        }

        public bool ImportXML(string xml)
        {
            if (!base.ImportXML(xml))
            {
                //fault in given xml
                //if (!base.ImportXML(SettingData.default_xml))
                //{
                //fault in default xml

                //Has defaults built in 3/4/2018
                this.data = new LoggerState();
                return false;
                //}
            }
            return true;
        }

        public void Reload()
        {
            Load(data.settingspath);
        }

        public override string ToString()
        {
            string NL = "\r\n";

            return "Channel:\t" + data.n_channels + " x " + data.n_devices + NL +
                    "Clip:\t\t" + data.clipsOn + NL +
                    "M:\t\t" + (data.mass + 1) + NL +
                    "Load:\t\t" + data.load + NL +
                    "Shaker:\t\t" + data.shakertype + NL +
                    "Pad:\t\t" + data.paddtype;
                    //"Duration: " + data.duration + NL +
                    //"Samples: " + data.n_samples + NL+
                    //"Timer: " + data.timer_interval;
        }

        public string GetHeader()
        {
            String header = "";
            header += data.version + ",";
            header += data.n_samples + ",";
            header += data.n_devices + ",";
            header += data.frequency + ",";
            header += (data.mass + 1) + ",";
            header += data.load + ",";
            header += (data.clipsOn ? 1 : 0) + ",";
            header += data.n_channels + ",";

            header += data.shakertype + ",";
            header += data.paddtype + ",";
            header += data.duration + ",";
            header += data.timer_interval;

            return header;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        /// <throws>FormatExceptions</throws>
        static public LoggerState LoadHeader(string line)
        {
            LoggerState sd = new LoggerState();

            string[] paras = line.Split(',');

            if (paras.Length != 11)
            {
                throw new FormatException("Incorrect header size");
            }

            //sd.n_samples = int.Parse(paras[0]);
            sd.n_devices = int.Parse(paras[1]);
            sd.frequency = int.Parse(paras[2]);
            sd.mass = int.Parse(paras[3]) - 1;
            sd.load = int.Parse(paras[4]);
            sd.clipsOn = (int.Parse(paras[5]) == 1);
            sd.n_channels = short.Parse(paras[6]);

            sd.shakertype = int.Parse(paras[7]);
            sd.paddtype = int.Parse(paras[8]);
            sd.duration = int.Parse(paras[9]);
            sd.timer_interval = short.Parse(paras[10]);

            return sd;
            
        }
    }
}
