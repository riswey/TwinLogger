using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Xml;


namespace MultiDeviceAIO
{

    class ProcessingSettings
    {
        public short n_channels { get; set; }
        public short timer_interval { get; set; }
        public short range { get; set; }
        public short n_samples { get; set; }

        //External parameters
        public float frequency { get; set; }
        public bool clipsOn { get; set; }

        //Original parameters
        public string path { get; set; }
        public bool modified { get; set; }
        public bool[] channels { get; set; }
        public int duration { get; set; }
        public int parameter { get; set; }
        public int window { get; set; }
    }

    class Settings
    {
        static string default_xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><ProcessingSettings xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><nchannels>16</nchannels><nticks>50</nticks><path>(default)</path><modified>false</modified><channels><boolean>true</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean><boolean>false</boolean></channels><range>0</range><duration>1</duration><parameter>1</parameter><window>0</window></ProcessingSettings>";

        public ProcessingSettings settings { get; set; }

        Settings() { }

        Settings(string path)
        {
            Load(path);
        }

        public void Load(string path)
        {
            string xml;
            try
            {
                xml = File.ReadAllText(path);
            }
            catch
            {
                xml = default_xml;
            }

            this.settings = deserialise(xml);
        }

        public bool Save(string path)
        {
            this.settings.modified = false;
            string xml = serialise(this.settings);

            try
            {
                File.WriteAllText(path, xml);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string serialise(ProcessingSettings ps1)
        {
            //Save
            XmlSerializer xsSubmit = new XmlSerializer(typeof(ProcessingSettings));

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, ps1);

                    return sww.ToString();
                }
            }
            return default_xml;
        }

        private ProcessingSettings deserialise(string xml)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(ProcessingSettings));

            using (var srr = new StringReader(xml))
            {
                using (XmlReader reader = XmlTextReader.Create(srr))
                {
                    return xsSubmit.Deserialize(reader) as ProcessingSettings;
                }
            }
        }

    }
}
