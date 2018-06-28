using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;


namespace UnitTestProject1
{
    public class Demo
    {
        public int one { get; } = 1;
        public string two { get; } = "a toucan";
        public double threepoint { get; } = 3.14;
    }

    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void MapClassString()
        {
            Demo d = new Demo();

            string test = "This is {ONE}. This is {TWO}. This is {THREEPOINT}";

            string res = LoggerState.MergeObjectToString(d, test);

            Debug.WriteLine(res);

            Assert.IsTrue(res.Equals("This is 1. This is a toucan. This is 3.14") );

        }

        [TestMethod]
        public void CSVLoadingCal()
        {
            IO.ReadCSV<double>(@"C:\Users\Alva\Desktop\JIM\cal_vals.csv", IO.DelegateParseDouble<double>, out List<List<double>> data);

            Assert.IsTrue(data.Count.Equals(40));
            Assert.IsTrue(data[0].Count.Equals(7));
            Assert.IsTrue(data[18][0].Equals(19));

        }
        /*
        [TestMethod]
        public void CSVLoadingJJD()
        {

            string fn = @"C:\Users\Alva\Desktop\JIM\M1.jjd";
            int n_channels = 64;

            //depricated
            IO.ReadCSVConcatColumns(fn, ',', n_channels, out DATA data, true);

            IO.ReadCSV<int>(fn, IO.DelegateParseInt<int>, out List<List<int>> jjdarray,',',true);
            IO.ConvertJJD2DATA(jjdarray, n_channels, out DATA data2);

            Assert.IsTrue(data.Count == data2.Count);
            Assert.IsTrue(data[0].Count == data2[0].Count);

            for (short i=0;i<data.Count;i++)
            {
                for (int j = 0; j < data[i].Count; j++)
                {
                    Assert.IsTrue(data[i][j] == data2[i][j]);

                }

            }

            Assert.IsTrue(data[0][3200] == data2[0][3200]);
            Debug.WriteLine(data[0][3200] + "==" + data2[0][3200]);

            Assert.IsTrue(data[1][12000] == data2[1][12000]);
            Debug.WriteLine(data[1][12000] + "==" + data2[1][12000]);


        }
        */
    }
}
