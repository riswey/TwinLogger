using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDeviceAIO
{

    //Class to be reference type!
    public class SettingData
    {
        public static string default_xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><SettingData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><n_channels>64</n_channels><duration>5</duration><n_samples>5000</n_samples><timer_interval>1000</timer_interval>./<testpath></testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>1</mass><load>0</load><shakertype>1</shakertype><paddtype>1</paddtype><path>current.xml</path><modified>false</modified></SettingData>";

        public string testpath { get; set; }        //Path to test data
        public string temp_filename { get; set; }   //last temp filename (recover)

        //Test parameters
        public float frequency { get; set; }
        public bool clipsOn { get; set; }
        public int mass { get; set; }
        public double load { get; set; }
        public int shakertype { get; set; }
        public int paddtype { get; set; }

        //Sampling parameters
        public int n_devices { get; set; }
        public short n_channels { get; set; }
        public int duration { get; set; }
        public short timer_interval { get; set; }
        public short n_samples                  //number channels scans before stop
        {
            get
            {
                return (short)(duration * timer_interval);
            }
        }
        public bool external_trigger { get; set; }

        //Internal parameters
        public string path { get; set; }
        public bool modified { get; set; }
    }

    public class AIOSettings : Settings<SettingData>
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

        public string ToString()
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

    }
}
