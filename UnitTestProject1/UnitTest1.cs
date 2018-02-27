using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsFormsApp1;
using System.Collections.Generic;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void IsFindingDevices()
        {
            List<MyAIO> devices = MyAIO.getDeviceList();

            Assert.IsTrue(devices.Count > 0);

        }


    }
}
