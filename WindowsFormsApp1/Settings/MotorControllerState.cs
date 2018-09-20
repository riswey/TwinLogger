using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDeviceAIO
{
    partial class LoggerState
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

        private long start_t { get; set; }



        //RM (Req. Min/Max) Timer pause
        //Put a timer block on Min/Max calls
        //Start/SetFreq reset min/max which takes 2s to stabilise
        const long RM_TIMER_PERIOD = 2000;
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

    }
}
