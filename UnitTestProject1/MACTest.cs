using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MotorController;

namespace UnitTestProject1
{
    [TestClass]
    public class MACTest
    {
        /*
         * At end of this we ned to be able to reset and resize buffer, window
         * 
         * 
         * 
         */



        [TestMethod]
        public void TestMovingStats()
        {
            MovingStatsOptimal mso = new MovingStatsOptimal(5);

            Assert.IsTrue(mso.MA == 0);
            Assert.IsTrue(mso.STD == 0);
            Assert.IsTrue(mso.RegressionB == 0);

            mso.Add(1);
            Assert.AreEqual(mso.MA, 0.2, 0.0001);
            Assert.AreEqual(mso.STD, 0.447214, 0.0001);
            Assert.AreEqual(mso.RegressionB, 0.2, 0.0001);

            mso.Add(8);
            Assert.AreEqual(mso.MA, 1.8, 0.0001);
            Assert.AreEqual(mso.STD, 3.49285, 0.0001);
            Assert.AreEqual(mso.RegressionB, 1.7, 0.0001);

            mso.Add(-3);
            Assert.AreEqual(mso.MA, 1.2, 0.0001);
            Assert.AreEqual(mso.STD, 4.08656, 0.0001);
            Assert.AreEqual(mso.RegressionB, 0.2, 0.0001);

            mso.Add(-5);
            Assert.AreEqual(mso.MA, 0.2, 0.0001);
            Assert.AreEqual(mso.STD, 4.96991, 0.0001);
            Assert.AreEqual(mso.RegressionB, -1.4, 0.0001);

            mso.Add(2);
            Assert.AreEqual(mso.MA, 0.6, 0.0001);
            Assert.AreEqual(mso.STD, 5.02991, 0.0001);
            Assert.AreEqual(mso.RegressionB, -1.1, 0.0001);

            mso.Add(2);
            Assert.AreEqual(mso.MA, 0.8, 0.0001);
            Assert.AreEqual(mso.STD, 5.06952, 0.0001);
            Assert.AreEqual(mso.RegressionB, -0.7, 0.0001);

        }

        float target = 1;

        [TestMethod]
        public void TestMovingCrosses()
        {
            MovingStatsCrosses mso = new MovingStatsCrosses(5, () => { return target; });
            //starts at 0
            mso.Add(3);
            //00003
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 3);
            Assert.AreEqual(mso.Crosses, 1);

            mso.Add(0);
            //00030
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 3);
            Assert.AreEqual(mso.Crosses, 2);

            //No cross
            mso.Add(1);
            //00301
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 3);
            Assert.AreEqual(mso.Crosses, 2);

            //Now cross (dot-slash rule)
            mso.Add(2);
            //03012
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 3);
            Assert.AreEqual(mso.Crosses, 3);

            //Cross (but trailing edge lost a cross)
            mso.Add(0);
            //30120
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 3);
            Assert.AreEqual(mso.Crosses, 3);
            //Cross (but trailing edge lost a cross)

            mso.Add(5);
            //01205
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 5);
            Assert.AreEqual(mso.Crosses, 3);

            mso.Add(5);
            //12055
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 5);
            Assert.AreEqual(mso.Crosses, 3);

            mso.Add(5);
            //20555
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 5);
            Assert.AreEqual(mso.Crosses, 2);

            mso.Add(5);
            //05555
            Assert.AreEqual(mso.Min, 0);
            Assert.AreEqual(mso.Max, 5);
            Assert.AreEqual(mso.Crosses, 1);

            mso.Add(5);
            //55555
            Assert.AreEqual(mso.Min, 5);
            Assert.AreEqual(mso.Max, 5);
            Assert.AreEqual(mso.Crosses, 0);

        }
    }
}