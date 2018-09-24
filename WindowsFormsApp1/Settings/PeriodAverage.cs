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

    class MovingAverage {
        protected float[] buffer;
        protected int size, head;
        private float sum, sum2;
        float MA {
            get {
                //Sum(X) / N
                return sum / size;
            }
        }
        float MA2 {
            get {
                //Sum(X^2)/N
                return sum2 / size;
            }
        }
        float MSD {
            get
            {
                //Sum(X^2)/N - mean^2
                return MA2 - (float)Math.Pow(MA, 2);
            }
        }


        public MovingAverage(int size)
        {
            sum = 0;
            size = size;
            head = 0;
            buffer = new float[size];
        }

        public void Add(float item)
        {
            sum = sum + (item - buffer[head]);
            sum2 = sum2 + (float)Math.Pow(item - buffer[head],2);
            buffer[head] = item;
            head = (++head) % size;
        }
    }

    /*
     * TODO: how to remove crosses from the windows trailing boundary
     */
    class MovingAverageCrosses: MovingAverage
    {
        float target { get; set; }

        public int crosses { get; private set; }
        float _max = 0;
        public float max_v_target {
            get {
                return _max - target;
            }
            private set {
                _max = value;
            }
        }

        float _min = float.MaxValue;
        public float min_v_target {
            get {
                return _min - target;
            }
            private set
            {
                _min = value;
            }
        }

        public MovingAverageCrosses(int size, float target) : base(size)
        {
            target = target;
        }

        public new void Add(float item)
        {
            if (
                (buffer[head] < target && item > target)
                ||
                (buffer[head] > target && item < target)
                )
            {
                //cross
            }

            max_v_target = Math.Max(item, max_v_target);
            min_v_target = Math.Min(item, min_v_target);

            base.Add(item);
        }


    }
}
 