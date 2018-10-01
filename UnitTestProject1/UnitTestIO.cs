using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiDeviceAIO;

namespace UnitTestProject1
{
    class Me
    {
        public int somevalue = 5;
    }



    [TestClass]
    public class UnitTestIO
    {

        [TestMethod]
        public void TestDirExist()
        {
            string path = @"c:\Users\Alva\Desktop\dummytest";

            Assert.IsTrue(IO.DirExists(path));


        }

        [TestMethod]
        public void TestRef()
        {

            Me me = new Me();
            T t = new T(me);

            me.somevalue = 10;

            Assert.IsTrue(t.get() == me.somevalue);







        }

        class T
        {
            Me me;

            public T(Me me)
            {
                this.me = me;
            }

            public int get()
            {
                return me.somevalue;
            }
        }


    }
}
