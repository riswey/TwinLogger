using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotorController
{
    class PeriodAverage
    {
        List<float> buffer = new List<float>();

        public void Add(float f) { buffer.Add(f); }

        public float GetPeriodAverage()
        {
            //sum
            //divide by count
            float average = 0;
            buffer.Clear();
            return average;
        }
    }
}
