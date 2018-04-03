using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

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

            string res = SettingData.MergeObjectToString(d, test);

            Debug.WriteLine(res);

            Assert.IsTrue(res.Equals("This is 1. This is a toucan. This is 3.14") );

        }
    }
}
