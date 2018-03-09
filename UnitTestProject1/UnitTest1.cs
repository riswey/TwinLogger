using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;
using System.Collections.Generic;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void IsFindingDevices()
        {
            List<MyAIO> devices = MyAIO.GetDeviceList();

            Assert.IsTrue(devices.Count > 0);

        }


    }
}
