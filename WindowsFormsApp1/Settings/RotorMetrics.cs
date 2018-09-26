using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotorController
{
    /*
     * Generates the average of the buffer then resets.
     */
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

    class MovingStatsOptimal
    {
        protected float[] buffer;
        protected int size, head;
        private float sum, 
            sum2,
            sumxy,
            regress_denom;

        public MovingStatsOptimal(int size)
        {
            sum = 0;
            this.size = size;
            head = 0;
            buffer = new float[size];
            /*
             * Simplify:
             * Sum(x - mean_x)^2
             * where x = {0,...,n-1}
             */
            regress_denom = size / 12 * (size ^ 2 - 1);
        }

        public float MA
        {
            get
            {
                //Sum(X) / N
                return sum / size;
            }
        }
        public float STD
        {
            get
            {
                //N s^2 = Sum[(y - meany)^2]
                //s^2 = Sum(y^2)/N - meany^2
                return (float)Math.Sqrt(sum2 / size - (float)Math.Pow(MA, 2));
            }
        }
        /*
         * numerator = Sum[(x - meanx)(y - meany)]
         * Expand and simplify -> sumx (1-n)/2 + sumxy where x = {0,...,N-1}
         * 
         * denom = Sum[(x-meanx)^2] = size / 12 * (size^2 - 1)
         *  where x = {0,..,N-1}
         */
        public float RegressionB
        {
            //TODO: sumxy is wrong!
            get
            {
                double numerator = sum * (1 - size) / 2 + sumxy;
                return (float)numerator / regress_denom;
            }
        }

        public void Add(float value)
        {
            float   old_value = buffer[head],
                    old_average = MA,
                    diff = value - old_value;
            sum += value - old_value;
            sum2 += value * value - old_value * old_value;
            sumxy += head * value - head * old_value;           //Woa this doesn't work!
                                                                //All values need be left-shifted 1!!!!
            buffer[head] = value;
            head = (++head) % size;
        }
    }

    /*
     * Optimise!
     * Calculates entire buffer stats on each add!
     */
    class MovingStatsCrosses : MovingStatsOptimal
    {
        public delegate float D_GetTarget();
        D_GetTarget GetTarget;                  //essentially a reference to target
        public int Crosses { get; private set; }
        public float Max { get; private set; } = 0;
        public float Min { get; private set; } = float.MaxValue;

        //MovingStatsCrosses(size, () => {return target;} )
        public MovingStatsCrosses(int size, D_GetTarget gettarget) : base(size)
        {
            this.GetTarget = new D_GetTarget(gettarget);
        }

        private void MeasureBuffer()
        {
            Crosses = 0;
            Min = float.MaxValue;
            Max = 0;

            float v1, v2;
            for (int i = 0; i < size; i++)
            {
                v1 = buffer[(head + i + 1) % size];
                v2 = buffer[(head + i + 2) % size];
                if (
                (v1 < GetTarget() && v2 > GetTarget())
                ||
                (v1 > GetTarget() && v2 < GetTarget())
                )
                {
                    Crosses++;
                }

                Max = Math.Max(v1, Max);
                Min = Math.Min(v1, Min);
            }
        }

        public new void Add(float item)
        {
            base.Add(item);
            MeasureBuffer();
        }

        public List<string> BoundPropertiesForUpdate { get; set; } = new List<string>();
    }
        
}
 