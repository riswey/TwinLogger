using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Collections.Generic;
using System.Diagnostics;
using CaioCs;


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
            AIOSettings s = new AIOSettings();

            s.ImportXML(SettingData.default_xml);

            MyAIO aio = new MyAIO(s.data);
            aio.DiscoverDevices("Aio00");

            List<float[]> ss = aio.ChannelsSnapShot();

            Debug.Write("####################################################################");
            Debug.Write(ss[0][0] + "," + ss[0][1] + "," + ss[0][2] + "," + ss[0][3] + "," + ss[0][4]);
            Debug.Write(ss[1][0] + "," + ss[1][1] + "," + ss[1][2] + "," + ss[1][3] + "," + ss[1][4]);

        }


        [TestMethod]
        public void TestSettingsImportExport()
        {
            //Import/export XML

            string testXML = SettingData.default_xml;

            Settings<SettingData> s = new Settings<SettingData>();

            s.ImportXML( testXML );
            Assert.IsTrue(s.data.n_channels == 64);

            string getXML = s.ExportXML();

            Debug.WriteLine(testXML);
            Debug.WriteLine(getXML);

            Assert.IsTrue(testXML == getXML);

        }

        [TestMethod]
        public void TestSettingsFiling()
        {

            string testXML = SettingData.default_xml;

            Settings<SettingData> s = new Settings<SettingData>();
            s.ImportXML(testXML);
            Assert.IsTrue(s.data.n_channels == 64);
            s.data.n_channels = 32;
            s.Save("test.xml");

            Settings<SettingData> t = new Settings<SettingData>();
            t.Load("test.xml");
            Assert.IsTrue(t.data.n_channels == 32);

        }



    }
}
