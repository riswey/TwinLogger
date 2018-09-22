using System;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

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
    /// <summary>
    /// Wrap a generic data class with disk persistance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilePersistentState<T>
    {
        public T data { get; set; }

        public FilePersistentState()
        {
            //Need to manually import settings!
        }

        public FilePersistentState(T data) 
        {
            this.data = data;
        }

        /// <summary>
        /// Load from external XML
        /// </summary>
        /// <param name="xml"></param>
        /// <returns>
        /// false if fails
        /// </returns>
        public bool ImportXML(string xml)
        {
            try
            {
                this.data = Deserialise(xml);
                return true;
            }
            catch (InvalidOperationException) { return false; }
            catch (XmlException) { return false; }
        }

        /// <summary>
        /// Generate XML for external
        /// </summary>
        /// <param name="xml">initialise to xml</param>
        /// <returns>
        /// false if fails
        /// </returns>
        public bool ExportXML(out string xml)
        {
            try
            {
                xml = Serialise(this.data);
                return true;
            }
            catch (InvalidOperationException) { xml = null; return false; }
            catch (XmlException) { xml = null; return false; }
        }

        //Load external path
        public bool Load(string path)
        {
            string xml;
            try
            {
                xml = File.ReadAllText(path);
                this.data = Deserialise(xml);
                return true;
            }
            catch (XmlException) { return false; }
            catch (IOException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (Exception) { throw; }
        }

        //Save to path in settings
        public bool Save(string path)
        {
            string xml;
            try
            {
                xml = Serialise(this.data);
                File.WriteAllText(path, xml);
                return true;
            }
            catch (XmlException) { return false; }
            catch (IOException) { return false; }
            catch (InvalidOperationException) { return false; }     //had a fit with XML changes. Ignore.
            catch (Exception) { throw; }
        }

        private string Serialise(T ps1)     //throws XmlException
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
        }

        private T Deserialise(string xml)       //throws XmlException
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));

            using (var srr = new StringReader(xml))
            {
                using (XmlReader reader = XmlTextReader.Create(srr))
                {
                    return (T) xsSubmit.Deserialize(reader);
                }
            }
        }

    }
}
