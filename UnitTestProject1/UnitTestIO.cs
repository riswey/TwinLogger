using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTestIO
    {
 
        [TestMethod]
        public void TestDirExist()
        {
            string path = @"c:\Users\Alva\Desktop\dummytest";

            Assert.IsTrue(Dir.Exists(path));


        }
    }
}
