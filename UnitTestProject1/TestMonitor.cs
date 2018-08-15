using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace UnitTestProject1
{
    [TestClass]
    public class TestMonitor
    {
        [TestInitialize]
        public void Init()
        {
            string fnMAPPING = @"C:\Users\Alva\Desktop\JIM\mapping.csv";
            //string mappingpath = @"C:\Users\Alva\source\repos\TTi\TwinLogger\UnitTestProject1\mapping.csv";
            int n_channels = 64;

            /////// Map (create Accelerometer/Channel tree)
            try
            {
                List<List<int>> mapping;
                IO.ReadCSV<int>(fnMAPPING, IO.DelegateParseInt<int>, out mapping, ',', true);
                Accelerometer.ImportMapping(mapping, n_channels);
            }
            catch (FileNotFoundException e)
            {
                throw new Exception("Cannot find mapping.csv");
            }

            /////// Calibrate

            string calpath = @"C:\Users\Alva\Desktop\JIM\cal_vals.csv";
            try
            {
                IO.ReadCSV<double>(calpath, IO.DelegateParseDouble<double>, out List<List<double>> caldata);
                Accelerometer.ImportCalibration(caldata);
            }
            catch (IOException ex)
            {
                throw new Exception("Error loading file" + ex.Message);
            }

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
        public void BasicTestChannels()
        {
            //If not set just returns false
            //string fnCalibration = @"C:\Users\Alva\Desktop\JIM\cal_vals.csv";
            //IO.ReadCSV<double>(fnCalibration, IO.DelegateParseDouble<double>, out List<List<double>> caldata);

            //Accelerometer.ImportCalibration(caldata);

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
            //ZERO, GAIN, VALUE
            Channel c = new Channel(1, 10, 10000, 1000, 10000);

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

            int mid_acc = (int)Math.Floor((double)Accelerometer.Count / 2) + 1;    //below on left, equal+ on right (1 based not 0 -> +1)
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

        [TestMethod]
        public void TestArrayStatus()
        {
            //Ordered Accelerometers 1,2,3,...,n (ch1,ch2,ch3 gvalues)
            var testdata = new double[] {
                -0.01, 0.65, 0.07,
                -0.02, 0.77, 0.01,
                0, 0.34, 0.04,
                -0.02, 0.3, 0.03,
                0.01, 0.31, 0.05,
                -0.04, 0.33, 0.05,
                0.03, 0.37, 0.06,
                0.02, 0.38, 0.04,
                0.05, 0.31, 0.06,
                0.02, 0.32, 0.05,
                0.02, 0.34, 0.01,
                0, 0.35, 0.04,
                0, 0.34, 0.05,
                0.05, 0.34, 0.04,
                0.01, 0.34, 0.02,
                -0.02, 0.37, 0.03,
                -0.01, 0.34, 0.03,
                -0.01, 0.38, 0.05,
                0.01, 0.35, 0.05,
                -0.01, 0.35, 0.03,
                0.06, -1.03, 0.01,
                0, -1.03, 0.04,
                0.02, -1, -0.01,
                0, -1, -0.07,
                0.01, -0.98, -0.05,
                0.03, -1, -0.07,
                0, -1.02, -0.02,
                0.01, -1.03, -0.04,
                0, -1, -0.02,
                0.02, -1.03, -0.03,
                0.03, -1, -0.04,
                0.05, -1, 0,
                -0.01, -1, -0.04,
                0.01, -0.99, -0.03,
                -0.25, -1.18, -0.31,
                -0.03, -0.99, -0.07,
                0.03, -1.01, -0.01,
                0.02, -1.01, -0.03,
                0.04, -1.02, -0.01,
                -0.06, -1, -0.07,
                0.03, 0.33, 0.04,
                -0.04, -1.05, -0.03
            };
            int RESULT = 0;


            //gvalue = ((value - zero) / gain);
            //value = (gvalue * gain) + zero

            //go through all channels and manually update to test data
            foreach (KeyValuePair<int, Accelerometer> a in Accelerometer.accrs)
            {
                int n;
                double d, bdata;

                for (int i = 0; i < 3; i++)
                {
                    n = (a.Value.number - 1) * 3 + i;   //acc num 1 based -> -1
                    d = testdata[n];                //tracknumber 0 based
                    bdata = d * a.Value.channels[i].gain + a.Value.channels[i].zero;
                    a.Value.channels[i].value = (int)Math.Round(bdata, 0);
                }
            }

            string str = Accelerometer.toHTML();

            int status = Accelerometer.ArrayStatus();

            Assert.IsTrue(status == RESULT);

        }

        [TestMethod]
        public void TestChannelStatus()
        {
            //tolerance hardcoded = 0.1G
            //default: int zero = 38192, double gain = 2413.5, int value = 32768
            int zero = 10000;
            double gain = 2000;
            Channel c = new Channel(1, 8, zero, gain, 0);
            //gval = ((value - zero) / gain);
            //val = gval * gain + zero;

            c.value = (int)(-1.11 * gain + zero);
            Assert.IsTrue(c.State == 1);
            c.value = (int)(-1.1 * gain + zero);        //>= lower boundary included in range above
            Assert.IsTrue(c.State == 2);                //green
            c.value = (int)(-0.9 * gain + zero);
            Assert.IsTrue(c.State == 4);
            c.value = (int)(-0.1 * gain + zero);
            Assert.IsTrue(c.State == 8);                //green
            c.value = (int)(0.1 * gain + zero);
            Assert.IsTrue(c.State == 16);
            c.value = (int)(0.89 * gain + zero);
            Assert.IsTrue(c.State == 16);
            c.value = (int)(1 * gain + zero);
            Assert.IsTrue(c.State == 32);               //green
            c.value = (int)(1.1 * gain + zero);
            Assert.IsTrue(c.State == 64);

        }

        [TestMethod]
        public void TestAccStatus()
        {
            Accelerometer a = new Accelerometer(1,16,1,2,3);
            //default values
            int zero = 10000;
            double gain = 2000;
            for (int i=0;i<3;i++)
            {
                a.channels[i].zero = zero;
                a.channels[i].gain = gain;
            }

            //All state 2 = green
            a.channels[0].value = (int)(-1 * gain + zero);
            a.channels[1].value = (int)(-1 * gain + zero);
            a.channels[2].value = (int)(-1 * gain + zero);
            Assert.IsTrue(a.channels[0].State == 2);
            Assert.IsTrue(a.channels[1].State == 2);
            Assert.IsTrue(a.channels[2].State == 2);
            Assert.IsTrue(a.Status);

            a.channels[1].value = (int)(-1.11 * gain + zero);   //channel 1 -> state 1
            Assert.IsTrue(a.channels[1].State == 1);
            Assert.IsFalse(a.Status);

            a.channels[1].value = (int)(-0.89 * gain + zero);   //channel 1 -> state 4
            Assert.IsTrue(a.channels[1].State == 4);
            Assert.IsFalse(a.Status);

            //All state 8 = green
            a.channels[0].value = (int)(0 * gain + zero);
            a.channels[1].value = (int)(0 * gain + zero);
            a.channels[2].value = (int)(0 * gain + zero);
            Assert.IsTrue(a.channels[1].State == 8);
            Assert.IsTrue(a.Status);

            a.channels[1].value = (int)(0.1 * gain + zero);     //channel 1 -> state 16 (rounding errors if gain not integer upset the boundary.
            Assert.IsTrue(a.channels[1].State == 16);           //This can be state 8 with rounding errors
            Assert.IsFalse(a.Status);

            a.channels[1].value = (int)(0.89 * gain + zero);   //channel 1 -> state 16
            Assert.IsTrue(a.channels[1].State == 16);
            Assert.IsFalse(a.Status);

            //All state 32 = green
            a.channels[0].value = (int)(1 * gain + zero);
            a.channels[1].value = (int)(1 * gain + zero);
            a.channels[2].value = (int)(1 * gain + zero);
            Assert.IsTrue(a.channels[1].State == 32);
            Assert.IsTrue(a.Status);

            a.channels[1].value = (int)(1.1 * gain + zero);
            Assert.IsTrue(a.channels[1].State == 64);
            Assert.IsFalse(a.Status);


        }
    }
}
