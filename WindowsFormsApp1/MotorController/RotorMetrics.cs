using MultiDeviceAIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

    //TESTED: 26/09/2018 (not resize)
    public class MovingStatsOptimal : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void InvokePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        void UpdateBindings()
        {
            var p2update = new List<string>() { "MA", "STD", "Gradient", "Min", "Max", "Crosses" };
            p2update.ForEach(p => { InvokePropertyChanged(p); });
        }


        [MotorProperty]
        public float rotor_speed { get; set; }      //needed to complete trigger string merge variables
        protected float[] buffer;
        protected int size, head;
        private float sum,sum2,sumxy, regress_denom;
        public bool bufferfull = false;

        public MovingStatsOptimal(int size)
        {
            this.size = size;
            Reset();
        }

        public void Reset(Nullable<int> newsize = null)
        {
            size = newsize ?? size;

            buffer = new float[size];
            head = 0;
            sum = sum2 = sumxy = 0;

            /*
             * Simplify:
             * Sum(x - mean_x)^2
             * where x = {0,...,n-1}
            */
            regress_denom = (float)(size / 12f * (Math.Pow(size, 2) - 1f));
        }
        /*
        public void ResizeBuffer(int newsize)
        {
            
            float[] newbuffer = new float[newsize];
            
            //Trying out new coding style: keep it simple! So splitting into 2 cases
            if (newsize >= size)
            {
                Array.Copy(buffer, 0, newbuffer, newsize - size, size);
                head += newsize - size;
            }
            else
            {
                Array.Copy(buffer, size - newsize, newbuffer, 0, newsize);
                head += (2 * size - newsize) % size;
            }
            
            //TODO: convert stats to new size! Some would work some not
            Reset();

            size = newsize;
            buffer = newbuffer;
        }
        */
        [MotorProperty]
        public float MA
        {
            get
            {
                //Sum(X) / N
                return sum / size;
            }
        }
        [MotorProperty]
        public float STD
        {
            get
            {
                //N s^2 = Sum[(y - meany)^2]
                //s^2 = Sum(y^2)/N - meany^2
                double sum2_ma2 = (sum2 - size * (float)Math.Pow(MA, 2));
                if (sum2_ma2 < 0)
                {
                    Debug.WriteLine("MAC Error: STD sum2 - ma2 < 0 (probably rounding error): " + sum2_ma2);
                    return 0;
                }

                double eval = Math.Sqrt(sum2_ma2 / (size - 1));

                return (float) eval;
            }
        }
        /*
         * numerator = Sum[(x - meanx)(y - meany)]
         * Expand and simplify -> sumx (1-n)/2 + sumxy where x = {0,...,N-1}
         * 
         * denom = Sum[(x-meanx)^2] = size / 12 * (size^2 - 1)
         *  where x = {0,..,N-1}
        */
        [MotorProperty]
        public float Gradient           //Regression B
        {
            get
            {
                double numerator = sum * (1 - size) / 2 + sumxy;
                if (regress_denom == 0) return 0;
                return (float)numerator / regress_denom;
            }
        }
        /*
         * x  = {0,1,2,3}
         * y  = {a,b,c,d}
         * xy = { 0a + 1b + 2c + 3d }
         * => add e
         * y' = {b,c,d,e}
         * xy' ={0b + 1c + 2d + 3e}
         * everything shifts left = subtract b,c,d,... 
         * xy - newsum = xy - (b+c+d+e)
         * add (size - 1) e but already sub e so => size * e
         * xy = newsum + size * e
         */
        public void Add(float value)
        {
            rotor_speed = value;
            float   old_value = buffer[head],
                    old_average = MA,
                    diff = value - old_value;
            sum += value - old_value;
            sum2 += value * value - old_value * old_value;
            sumxy += -sum + size * value;
            buffer[head] = value;
            head = (++head) % size;
            bufferfull = bufferfull || (head == 0);
            UpdateBindings();
        }
    }

    //TESTED: 26/09/2018 (not resize)
    public class MovingStatsCrosses : MovingStatsOptimal
    {
        public delegate float D_GetTarget();
        D_GetTarget dtargetspeed;                  //essentially a reference to target wrapped in a function block
        [MotorProperty]
        public float target_speed { get
            {
                return dtargetspeed();
            }
        }
        [MotorProperty]
        public int Crosses { get; private set; }
        float _max = 0, _min = float.MaxValue;

        //NOTE public interface must not return false small values, only false large values to avoid false trigger
        [MotorProperty]
        public float Max {
            get {
                if (_max == 0) return float.MaxValue;
                return _max;
            }
        }
        [MotorProperty]
        public float Min
        {
            //NOTE: this will remain at zero until buffer full :. unfilled buffer values = 0
            //The alternative is to set to float.MaxValue and have max = this until buffer full.
            get
            {
                if (_min == float.MaxValue) return 0;
                return _min;
            }
        }

        //DOC: MovingStatsCrosses(size, () => {return target;} )
        public MovingStatsCrosses(int size, D_GetTarget gettarget) : base(size)
        {
            this.dtargetspeed = new D_GetTarget(gettarget);
        }

        public new void Reset(Nullable<int> newsize)
        {
            base.Reset(newsize);
            Crosses = 0;
            _max = 0;
            _min = float.MaxValue;
            bufferfull = false;
        }
        /*
        public new void ResizeBuffer(int newsize)
        {
            base.ResizeBuffer(newsize);
            //NOTE: no need to reset as crosses/min/max not effected by size (except trailing edge case!)
        }
        */
        private void MeasureBuffer()
        {
            Crosses = 0;
            _min = float.MaxValue;
            _max = 0;

            //Do min/max on position behind head
            _max = Math.Max(buffer[(head - 1 + size) % size], _max);
            _min = Math.Min(buffer[(head - 1 + size) % size], _min);

            float v1, v2;
            //size-1 intervals
            for (int i = 0; i < size - 1; i++)
            {
                //NOTE: start loop from head + 1 (head already at head + 1) i.e. x = 0
                v1 = buffer[(head + i) % size];
                v2 = buffer[(head + i + 1) % size];
                if (
                (v1 <= target_speed && v2 > target_speed)
                ||
                (v1 > target_speed && v2 <= target_speed)
                )
                {
                    Crosses++;
                }
                //HACKED
                //DOC: dot-slash (.5) rule
                //situation ./ is cross if starts on target, not if ends on target
                //situation \. is cross if ends on target, not if starts on target
                //TODO: code 1st order edge case
                //-> V or n situation = no cross. Only X situation.
                //if v1 == target then look v0 (if exists)
                //if v2 == target then look v3 (if exists)

                _max = Math.Max(v1, _max);
                _min = Math.Min(v1, _min);
            }

        }

        public new void Add(float item)
        {
            base.Add(item);
            MeasureBuffer();
        }


    }
        
}
 