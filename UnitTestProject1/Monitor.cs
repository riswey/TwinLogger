using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Collections.Generic;
using System.Diagnostics;


namespace UnitTestProject1
{
    [TestClass]
    public class Monitor
    {
        [TestInitialize]
        public void Init()
        {
            string fnMAPPING = @"C:\Users\Alva\Desktop\JIM\mapping.csv";
            int n_channels = 64;

            Dictionary<int, Accelerometer> accrs = new Dictionary<int, Accelerometer>();

            IO.ReadCSV<int>(fnMAPPING, IO.DelegateParseInt<int>, out List<List<int>> mapping,',',true);

            Accelerometer.ImportMapping(mapping, n_channels);

        }

        [TestMethod]
        public void LoadAccrs()
        {
            //Check mapping worked
            Assert.IsTrue(Accelerometer.Count == 42);

            Assert.IsTrue(Accelerometer.accrs[4].channels[2].tracknum == 12);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].devicenum == 0);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].devicechannel == 12);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].zero == 32768);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].gain == 2000);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].value == 32768);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].G == 0);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].State == 1);


            Assert.IsTrue(Accelerometer.accrs[36].channels[1].tracknum == 113);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].devicenum == 1);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].devicechannel == 49);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].zero == 32768);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].gain == 2000);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].value == 32768);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].G == 0);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].State == 1);

        }


        [TestMethod]
        public void Calibrate()
        {
            //If not set just returns false
            string fnCalibration = @"C:\Users\Alva\Desktop\JIM\cal_vals.csv";
            IO.ReadCSV<double>(fnCalibration, IO.DelegateParseDouble<double>, out List<List<double>> caldata);

            Accelerometer.ImportCalibration(caldata);

            Assert.IsTrue(Accelerometer.accrs[4].channels[2].zero == 38189);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].gain == 2460.9);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].value == 32768);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].G == -2);
            Assert.IsTrue(Accelerometer.accrs[4].channels[2].State == 0);

            Assert.IsTrue(Accelerometer.accrs[36].channels[1].zero == 38178);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].gain == 2393.1);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].value == 32768);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].G == -2);
            Assert.IsTrue(Accelerometer.accrs[36].channels[1].State == 0);
        }

        [TestMethod]
        public void ImportValues()
        {
            List<int[]> snapshot = new List<int[]>();
            snapshot.Add(new int[] { 30000, 31000, 32000, 33000 });
            snapshot.Add(new int[] { 40000, 41000, 42000, 43000 });

            Accelerometer.setChannelData(snapshot);

            Assert.IsTrue(Accelerometer.accrs[1].channels[0].value == 30000);
            Assert.IsTrue(Accelerometer.accrs[1].channels[1].value == 31000);
            Assert.IsTrue(Accelerometer.accrs[1].channels[2].value == 32000);
            Assert.IsTrue(Accelerometer.accrs[2].channels[0].value == 33000);

            Assert.IsTrue(Accelerometer.accrs[21].channels[0].value == 40000);
            Assert.IsTrue(Accelerometer.accrs[21].channels[1].value == 41000);
            Assert.IsTrue(Accelerometer.accrs[21].channels[2].value == 42000);
            Assert.IsTrue(Accelerometer.accrs[22].channels[0].value == 43000);

        }

        [TestMethod]
        public void TestChannel()
        {
            Channel c = new Channel(1,10,10000,1000,10000);

            Assert.IsTrue(c.G == 0);
            Assert.IsTrue(c.State == 1);
            c.value = 12000;
            Assert.IsTrue(c.G == 2);
            Assert.IsTrue(c.State == 2);
            c.value = 8000;
            Assert.IsTrue(c.G == -2);
            Assert.IsTrue(c.State == 0);
        }

        [TestMethod]
        public void TestAccLocs()
        {
            //Takes channel data + cal_data and organises a display of accelerometers

            //in 2 columns (?make dynamic on resize) width window?
            //An acceleromoeter is 3x channels (with 3 states). 3x METER_WIDTH = 100

            int mid_acc = (int)Math.Floor((double) Accelerometer.Count / 2) + 1;    //below on left, equal+ on right (1 based not 0 -> +1)
            const int ACC_WIDTH = 120;
            const int ACC_HEIGHT = 25;

            foreach (KeyValuePair<int, Accelerometer> accr in Accelerometer.accrs)
            {
                //Key is proper number (not this List row crap elsewhere)
                int x = (int)Math.Floor((double)accr.Key / mid_acc) * ACC_WIDTH;
                int y = accr.Key % mid_acc * ACC_HEIGHT;

                Debug.WriteLine(x + "," + y);
            }

        }


    }
}
