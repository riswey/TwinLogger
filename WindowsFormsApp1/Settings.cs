using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

using System.Diagnostics;

/*
 * To make this really reusable code: Settings should be independent of ProcessSettings!
 * 
 * Make Settings generic!
 * 
 * Also unhappy that Settings can be initialised without any data and effecively init muct be called after.
 * 
 * 
 */


namespace MultiDeviceAIO
{
    public class Settings<T>
    {
        public T data { get; set; }

        //Load from external XML
        public void ImportXML(string xml)
        {
            this.data = deserialise(xml);
        }

        //Generate XML for external
        public string ExportXML()
        {
            return serialise(this.data);
        }

        //Load external path
        public void Load(string path)
        {
            try
            {
                string xml = File.ReadAllText(path);
                this.data = deserialise(xml);
            }
            catch
            {
                throw;
            }
        }

        //Save to path in settings
        public void Save(string path)
        {
            string xml = serialise(this.data);

            try
            {
                File.WriteAllText(path, xml);
            }
            catch
            {
                throw;
            }
        }

        private string serialise(T ps1)
        {
            //Save
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, ps1);

                    return sww.ToString();
                }
            }
            throw new Exception("Serialisation failed");
        }

        private T deserialise(string xml)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));

            using (var srr = new StringReader(xml))
            {
                using (XmlReader reader = XmlTextReader.Create(srr))
                {
                    return (T) xsSubmit.Deserialize(reader);
                }
            }
            throw new Exception("Deserialisation failed");
        }

    }
}
