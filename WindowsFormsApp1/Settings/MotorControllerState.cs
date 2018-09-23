using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MultiDeviceAIO
{
    public partial class LoggerState
    {
        
        public static long GetTime()   //millisecond time
        {
            return (long)Math.Round(DateTimeOffset.Now.UtcTicks / 10000.0d, 0);
        }

        public float p { get; set; }
        public float i { get; set; }
        public float d { get; set; }
        public int pulse_delay { get; set; }
        public long min_period { get; set; }
        public long max_period { get; set; }
        public float target_speed { get; set; }
        
        //TODO: add to period 
        public float rotor_speed { get; set; }

        MotorController.PeriodAverage pa = new MotorController.PeriodAverage();
        float rotor_speed_ma
        {
            get
            {
                //TODO:
                //MA for rotor period
                return rotor_speed;
            }
        }
        
        #region MinMax_TIMER
        //RM (Req. Min/Max) Timer pause
        //Put a timer block on Min/Max calls
        //Start/SetFreq reset min/max which takes 2s to stabilise
        //[NonSerialized]
        const long RM_TIMER_PERIOD = 2000;
        private long start_t { get; set; }
        long rm_timer { get; set; } = 0;
        
        public void StartRMTimer()
        {
            //Start/SetFreq events reset the MaxMin buffer
            //Takes time to give meaningful results
            rm_timer = GetTime();
        }

        public bool IsRMDisabled()
        {
            return (GetTime() - rm_timer) > RM_TIMER_PERIOD;
        }

        //Seems this is just for logging
        public bool IsMMInRange()
        {
            return min_period != 1E7 && max_period != 0;
        }

        #endregion
        
        #region LOGGING

        /*
        public string path { get; set; }
        //Stores the start time (motor logs are recorded relative)
        public void Start(string path = "")
        {
            this.path = path;
            doWrite("-------------------------------------------\r\nt\tTarget\tActual\tP\tI\tD\tDelay\tMin\tMax");
            start_t = GetTime();
        }

        private void doWrite(string str)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path + "Log.dat", true))
            {
                file.WriteLine(str);
            }

        }

        public void Write()
        {
            if (!IsMMInRange())
            {
                //Don't log if min/max still at starting values
                return;
            }

            long millisecs = GetTime() - start_t;

            string str = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                millisecs,
                target_speed,
                pa.GetPeriodAverage(),
                p,
                i,
                d,
                pulse_delay,
                min_period,
                max_period
            );

            //doWrite(str);
        }
        */

        #endregion

        /////////////////////////////////////////////////////////////////////
        //
        // NEW CODE
        //
        
        //Don't forget period average on rotor speed

        [XmlIgnore]
        public DataTable dt = new DataTable();

        [XmlIgnore]
        private int x = 0;

        [XmlIgnore]
        float lowerspeed;
        [XmlIgnore]
        float upperspeed;

        public float tolerance { get; set; } = 1.1f;

        public void InitMotorControllerState()
        {
            dt.Columns.Add("X_Value", typeof(long));
            dt.Columns.Add("Target", typeof(float));
            dt.Columns.Add("Upper", typeof(float));
            dt.Columns.Add("Lower", typeof(float));
            dt.Columns.Add("Y_Value", typeof(float));
        }

        //[NonSerialized]
        public float graphrange = 50;

        void SetBounds()
        {
            lowerspeed = target_speed / tolerance;
            upperspeed = target_speed * tolerance;
        }
        
        public long timeout { get; set; } = 60000;    //1min
        [XmlIgnore]
        public long enterrange = 0;
        [XmlIgnore]
        public long stableperiod = 3000;

        public bool IsRotorInRange
        {
            get
            {
                if (rotor_speed > lowerspeed && rotor_speed < upperspeed)
                {
                    if (enterrange == 0)
                    {
                        //just entered
                        enterrange = GetTime();
                    }
                    return true;
                }
                else
                {
                    enterrange = 0;
                    return false;
                }
            }
        }
        
        private bool IsRotorStable
        {
            get
            {
                //NOTE: It calls IsRotorInRange -> ensures enterrange set
                return IsRotorInRange
                    &&
                        (enterrange != 0 && GetTime() - enterrange > stableperiod);
            }
        }

        public bool IsReadyToSample
        {
            get
            {
                //DOC: Samples after timeout whatever. This is the window for improved motor control
                return IsRotorStable || GetTime() - start_t > timeout;
            }
        }
    }
}
