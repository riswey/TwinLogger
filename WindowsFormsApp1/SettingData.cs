using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDeviceAIO
{
    public interface I_TestSettings
    {
        //Settings critical to AIO Sampling

        short n_channels { get; set; }
        short n_samples { get; set; }
        short timer_interval { get; set; }

        //To create file label
        float frequency { get; set; }
        bool clipsOn { get; set; }
        int mass { get; set; }
        double load { get; set; }
    }

    //Class to be reference type!
    public class SettingData : I_TestSettings
    {
        public static string default_xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><SettingData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><n_channels>64</n_channels><duration>5</duration><n_samples>5000</n_samples><timer_interval>1000</timer_interval><frequency>0</frequency><clipsOn>false</clipsOn><mass>1</mass><load>0</load><shakertype>1</shakertype><paddtype>1</paddtype><path>current.xml</path><modified>false</modified></SettingData>";


        public short n_channels { get; set; }
        public int duration { get; set; }
        public short n_samples { get; set; }
        public short timer_interval { get; set; }

        //External parameters
        public float frequency { get; set; }
        public bool clipsOn { get; set; }
        public int mass { get; set; }
        public double load { get; set; }
        public int shakertype { get; set; }
        public int paddtype { get; set; }


        //Internal parameters
        public string path { get; set; }
        public bool modified { get; set; }
    }

    class AIOSettings : Settings<SettingData>
    {

        public new void Load(string path)
        {
            base.Load(path);
            data.path = path;
            data.modified = false;
        }

        public new void Save(string path)
        {
            data.path = path;
            data.modified = false;
            base.Save(path);
        }

        public void Reload()
        {
            Load(data.path);
        }

    }
}
