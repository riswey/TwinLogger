﻿using System;
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

        void InitMotorControllerState()
        {
            int tickpermetricwindow = (int)Math.Ceiling((double)metric_window / arduinotick);
            mac = new MotorController.MovingStatsCrosses(tickpermetricwindow, () => { return target_speed; } );
            mac.BoundPropertiesForUpdate = new List<string>() { "MA","STD","Gradient", "Min", "Max","Crosses"};
        }

        public void ResetMAC()
        {
            //New tests must start history again
            mac.Reset();
        }

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
        float _target_speed = 50;
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

        //[XmlIgnore]
        //MotorController.PeriodAverage pa = new MotorController.PeriodAverage();
        MotorController.MovingStatsCrosses mac;

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


        [MotorProperty]
        public float MA { get { return mac.MA; } }
        [MotorProperty]
        public float STD { get { return mac.STD; } }
        [MotorProperty]
        public float Gradient { get { return mac.RegressionB; } }
        [MotorProperty]
        public float Min { get { return mac.Min; } }
        [MotorProperty]
        public float Max { get { return mac.Max; } }
        [MotorProperty]
        public int Crosses { get { return mac.Crosses; } }
        [XmlIgnore]
        float _rotor_speed = 0;
        [XmlIgnore]
        public float rotor_speed
        {
            get
            {
                return _rotor_speed;
            }
            set
            {
                _rotor_speed = value;
                dt.Rows.Add((int)RotorX, target_speed, target_speed + 5, target_speed - 5, value);
                mac.Add(value);
                InvokePropertyChanged("rotor_speed");
                mac.BoundPropertiesForUpdate.ForEach( p => { InvokePropertyChanged(p); } );
            }
        }

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

        [XmlIgnore]
        private DataTable _dt;
        [XmlIgnore]
        public DataTable dt
        {
            get
            {
                if (_dt == null)
                {
                    _dt = new DataTable();
                    _dt.Columns.Add("X_Value", typeof(long));
                    _dt.Columns.Add("Target", typeof(float));
                    _dt.Columns.Add("Upper", typeof(float));
                    _dt.Columns.Add("Lower", typeof(float));
                    _dt.Columns.Add("Y_Value", typeof(float));
                }
                return _dt;
            }
        }
        
        [XmlIgnore]
        public bool IsReadyToSample {
            get
            {

                bool eval = EvalTrigger;
                Debug.WriteLine("TEval: " + eval);
                Debug.WriteLine("TX>MW: " + TestX + ">" + metric_window);
                Debug.WriteLine("TX>TO: " + TestX + ">" + timeout);

                return (eval && TestX > metric_window)                  //allow full metrics to be processes 
                    || (TestX > timeout);                               //timeout
            }
        }

        #region EVAL TRIGGER 
        //TODO: expectiment with this
        //if it won't substutide variable, then we do it manually (like with path)
        public string metriccommand { get; set; } = "{UPPER} - {MAX} >0 AND {LOWER} - {MIN} < 0";

        public string TriggerMerged
        {
            get
            {
                string mergestring = MergeObjectToString<MotorPropertyAttribute>(this, metriccommand);
                return mergestring;
            }
        }

        public bool EvalTrigger {
            get {
                string smerged = TriggerMerged;

                Debug.WriteLine("Merged: " + smerged);

                return (bool)_dt.Compute(smerged, "");
            }
        }

        #endregion
    }
}
