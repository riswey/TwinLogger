using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Collections.Generic;
using System.Diagnostics;
using CaioCs;

using System.Threading;



namespace UnitTestProject1
{
    using DEVICEID = System.Int16;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void IsFindingDevices()
        {
            Caio aio = new Caio();

            List<DEVICEID> devices = new List<DEVICEID>();

            string device_name = "Aio00";

            long ret = 1;
            DEVICEID id1 = 0;
            string full_device_name;
            for (int i = 0; i < 10; i++)
            {
                full_device_name = device_name + i;
                ret = aio.Init(full_device_name, out id1);
                if (ret == 0)
                {
                    devices.Add(id1);
                }
            }
            aio = null;

            Assert.IsTrue(devices.Count == 2);

        }

        [TestMethod]
        public void SnapShot()
        {
            PersistentLoggerState s = new PersistentLoggerState();

            MyAIO aio = new MyAIO(true);
            aio.DiscoverDevices("Aio00");

            List<float[]> ss = aio.ChannelsSnapShot(s.data.n_channels);

            Debug.Write("####################################################################");
            Debug.Write(ss[0][0] + "," + ss[0][1] + "," + ss[0][2] + "," + ss[0][3] + "," + ss[0][4]);
            Debug.Write(ss[1][0] + "," + ss[1][1] + "," + ss[1][2] + "," + ss[1][3] + "," + ss[1][4]);

        }


        [TestMethod]
        public void TestSettingsImportExport()
        {
            //Import/export XML
            string testXMLfault = @"<?xmlversion=""1.0"" encoding=""utf-16""?><SettingData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><testpath>C:\Users\Alva\Desktop\Control Tests Device 2</testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>0</mass><load>0</load><shakertype>0</shakertype><paddtype>1</paddtype><n_devices>0</n_devices><n_channels>64</n_channels><duration>5</duration><timer_interval>1000</timer_interval><external_trigger>false</external_trigger><path>C:\Users\Alva\Desktop\default.xml</path><modified>false</modified></SettingData>";
            string testXML =      @"<?xml version=""1.0"" encoding=""utf-16""?><SettingData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><testpath>C:\Users\Alva\Desktop\Control Tests Device 2</testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>0</mass><load>0</load><shakertype>0</shakertype><paddtype>1</paddtype><n_devices>0</n_devices><n_channels>64</n_channels><duration>5</duration><timer_interval>1000</timer_interval><external_trigger>false</external_trigger><path>C:\Users\Alva\Desktop\default.xml</path><modified>false</modified></SettingData>";

            FilePersistentState<LoggerState> s = new FilePersistentState<LoggerState>();

            //How robust to exceptions
            bool res = s.ImportXML(testXMLfault);
            Assert.IsFalse(res);

            //On good xml
            res = s.ImportXML( testXML );
            Assert.IsTrue( res );
            Assert.IsTrue(s.data.n_channels == 64);

            string getXML;
            s.ExportXML(out getXML);

            Debug.WriteLine(testXML);
            Debug.WriteLine(getXML);

            Assert.IsTrue(testXML == getXML);

        }

        [TestMethod]
        public void TestAIOSettingsImportExport()
        {
            //string testXMLfault = @"<?xmlversion=""1.0"" encoding=""utf-16""?><SettingData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><testpath>C:\Users\Alva\Desktop\Control Tests Device 2</testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>0</mass><load>0</load><shakertype>0</shakertype><paddtype>1</paddtype><n_devices>0</n_devices><n_channels>64</n_channels><duration>5</duration><timer_interval>1000</timer_interval><external_trigger>false</external_trigger><path>C:\Users\Alva\Desktop\default.xml</path><modified>false</modified></SettingData>";
            string testXML = @"<?xml version=""1.0"" encoding=""utf-16""?><SettingData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><testpath>C:\Users\Alva\Desktop\Control Tests Device 2</testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>0</mass><load>0</load><shakertype>0</shakertype><paddtype>1</paddtype><n_devices>0</n_devices><n_channels>64</n_channels><duration>5</duration><timer_interval>1000</timer_interval><external_trigger>false</external_trigger><path>C:\Users\Alva\Desktop\default.xml</path><modified>false</modified></SettingData>";

            PersistentLoggerState aios = new PersistentLoggerState();

            bool res;
            //if comment out the default_xml line in code
            //res = aios.ImportXML(testXMLfault);
            //Assert.IsFalse(res);
            //Assert.IsTrue(aios.data.n_channels == 0);

            res = aios.ImportXML(testXML);
            Assert.IsTrue(res);
            Assert.IsTrue(aios.data.n_channels == 64);

        }


        [TestMethod]
        public void TestSettingsFiling()
        {
            string testXML = @"<?xml version=""1.0"" encoding=""utf-16""?><SettingData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><testpath>C:\Users\Alva\Desktop\Control Tests Device 2</testpath><frequency>0</frequency><clipsOn>false</clipsOn><mass>0</mass><load>0</load><shakertype>0</shakertype><paddtype>1</paddtype><n_devices>0</n_devices><n_channels>64</n_channels><duration>5</duration><timer_interval>1000</timer_interval><external_trigger>false</external_trigger><path>C:\Users\Alva\Desktop\default.xml</path><modified>false</modified></SettingData>";

            FilePersistentState<LoggerState> s = new FilePersistentState<LoggerState>();
            s.ImportXML(testXML);
            Assert.IsTrue(s.data.n_channels == 64);
            s.data.n_channels = 32;
            s.Save("test.xml");

            FilePersistentState<LoggerState> t = new FilePersistentState<LoggerState>();
            t.Load("test.xml");
            Assert.IsTrue(t.data.n_channels == 32);

        }

    }
}
