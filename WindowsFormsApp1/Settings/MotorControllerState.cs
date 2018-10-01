using MotorController;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

/*
 * TODO:
 * Be able to set the mac metric window when property changes
 * ? other dynamic changes
 * 
 * timerticks are in arduinotimer ticks. When set remember to divide
 * 
 */
namespace MultiDeviceAIO
{
    public class MotorPropertyAttribute : Attribute { }

    public partial class LoggerState
    {

        [XmlIgnore]
        public static long GetTime_ms //millisecond time
        {
            get 
            {
                return (long)Math.Round(DateTimeOffset.Now.UtcTicks / 10000.0d, 0);
            }
        }

        float _p = 0, _i = 0, _d = 0;
        //int _pulse_delay = 0;
        public float p { get { return _p; } set { _p = value; InvokePropertyChanged("p"); } }
        public float i { get { return _i; } set { _i = value; InvokePropertyChanged("i"); } }
        public float d { get { return _d; } set { _d = value; InvokePropertyChanged("d"); } }
        //public int pulse_delay { get { return _pulse_delay; } set { _pulse_delay = value; InvokePropertyChanged("pulse_delay"); } }
        //long _min_period = -1, _max_period = -1;
        /*
        public long min_period {
            get
            {
                return _min_period;
            }
            set
            {
                //Check that incoming is valid
                if (value < 1E7)
                {
                    _min_period = value;
                    InvokePropertyChanged("min_period");
                }
                else
                    _min_period = -1;
            }
        }
        public long max_period
        {
            get
            {
                return _max_period;
            }
            set
            {
                if (value > 0)
                {
                    _max_period = value;
                    InvokePropertyChanged("max_period");
                }
                else
                    _max_period = -1;
            }
        }
        */
        [XmlIgnore]
        float _rotor_speed = 0;
        [XmlIgnore]
        public float rotor_speed {
            get
            {
                return _rotor_speed;
            }
            set
            {
                _rotor_speed = value;
                InvokePropertyChanged("rotor_speed");
            }
        }

        float _target_speed = 20;
        [TestProperty]
        [MotorProperty]
        public float target_speed
        {
            get
            {
                return _target_speed;
            }
            set
            {
                _target_speed = value;
                InvokePropertyChanged("target_speed");
            }
        }

        #region ROTOR METRIC WINDOW

        public int metric_window { get; set; } = 3000;          //Length metric window
        
        //Motor t
        [XmlIgnore]
        private long _rotor_0 { get; set; }                      //Marker for start of rotor run
        [XmlIgnore]
        public long Rotor0 { get { return _rotor_0; } set { _rotor_0 = LoggerState.GetTime_ms; } }
        [XmlIgnore]
        public long RotorX { get { return LoggerState.GetTime_ms - Rotor0;  } }
        
        //Test t
        [XmlIgnore]
        private long _test_0 { get; set; }                      //Marker for start of test run
        [XmlIgnore]
        public long Test0 { get { return _test_0; } set {
                _test_0 = LoggerState.GetTime_ms;
            } }
        /// <summary>
        /// Used to determine trigger timeout
        /// Zero at start of test, before first trigger eval!
        /// </summary>
        [XmlIgnore]
        public long TestX { get { return LoggerState.GetTime_ms - Test0; } }
        /*
        [MotorProperty]
        public float MA { get { trigger.TryGetTarget(out TriggerLogic tl); return tl?.mac.MA ?? 0; } }
        [MotorProperty]
        public float STD { get { trigger.TryGetTarget(out TriggerLogic tl); return tl?.mac.STD ?? 0; } }
        [MotorProperty]
        public float Gradient { get { trigger.TryGetTarget(out TriggerLogic tl); return tl?.mac.RegressionB ?? 0; } }
        [MotorProperty]
        public float Min { get { trigger.TryGetTarget(out TriggerLogic tl); return tl?.mac.Min ?? 0; } }
        [MotorProperty]
        public float Max { get { trigger.TryGetTarget(out TriggerLogic tl); return tl?.mac.Max ?? 0; } }
        [MotorProperty]
        public int Crosses { get { trigger.TryGetTarget(out TriggerLogic tl); return tl?.mac.Crosses ?? 0; } }
        [XmlIgnore]
        float _rotor_speed = 0;
        */

        #endregion

        /*
        //No Min/Max period calls anymore
        #region Min/Max Rotor Period Request Delay
        //RM (Req. Min/Max) Timer pause
        //Put a timer block on Min/Max calls
        //Start/SetFreq reset min/max which takes 2s to stabilise
        [XmlIgnore]
        const long RM_TIMER_PERIOD = 2000;

        [XmlIgnore]
        long rm_timer { get; set; } = 0;        //Marker for delay in Min/Max calls

        public void StartRMTimer()
        {
            //Start/SetFreq events reset the MaxMin buffer
            //Takes time to give meaningful results
            rm_timer = GetTime_ms;
        }

        public bool IsRMDisabled()
        {
            return (GetTime_ms - rm_timer) > RM_TIMER_PERIOD;
        }
        
        #endregion
        */

        #region LOGGING

        public string path { get; set; }
        
        //Stores the text start time, preps log file
        public void RotorLogStart()
        {
            doWrite("-------------------------------------------\r\nt\tTarget\tActual\tP\tI\tD");
        }
        
        public void LogWrite()
        {

            string str = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                RotorX,
                target_speed,
                rotor_speed,
                p,
                i,
                d
            );

            doWrite(str);
        }

        private void doWrite(string str)
        {
            //TODO: need smart write which creates folder!
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(testpath + @"\rotor.log", true))
            {
                file.WriteLine(str);
            }
        }

        #endregion

        //TODO: timeout need to reset
        public long timeout { get; set; } = 60000;    //1min

        public string metriccommand { get; set; } = "{MAX} - {TARGET_SPEED} <0.05 AND {TARGET_SPEED} - {MIN}<0.05";

    }
}
