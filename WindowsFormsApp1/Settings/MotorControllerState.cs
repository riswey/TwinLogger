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

        void InitMotorControllerState()
        {
            mac = new MotorController.MovingStatsCrosses(metric_window, () => { return target_speed; } );
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
        int _pulse_delay = 0;
        public float p { get { return _p; } set { _p = value; InvokePropertyChanged("p"); } }
        public float i { get { return _i; } set { _i = value; InvokePropertyChanged("i"); } }
        public float d { get { return _d; } set { _d = value; InvokePropertyChanged("d"); } }
        public int pulse_delay { get { return _pulse_delay; } set { _pulse_delay = value; InvokePropertyChanged("pulse_delay"); } }
        long _min_period = -1, _max_period = -1;
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

        float _target_speed = 50;
        [TestProperty]
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
                SetBounds();
                graphrange = target_speed * 0.2f;
            }
        }

        #region ROTOR METRIC WINDOW

        //[XmlIgnore]
        //MotorController.PeriodAverage pa = new MotorController.PeriodAverage();
        MotorController.MovingStatsCrosses mac;

        const int arduinotimertick = 500;

        //In timer ticks. Stable windows is 3000ms (500ms arduinotimer)
        public int metric_window { get; set; } = 30;        //Length window in arduino_ticks

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
                dt.Rows.Add(x++, target_speed, Upper, Lower, value);
                mac.Add(value);
                InvokePropertyChanged("rotor_speed");
                mac.BoundPropertiesForUpdate.ForEach( p => { InvokePropertyChanged(p); } );
            }
        }

        #endregion

        #region Min/Max Rotor Period Request Delay
        //RM (Req. Min/Max) Timer pause
        //Put a timer block on Min/Max calls
        //Start/SetFreq reset min/max which takes 2s to stabilise
        [XmlIgnore]
        const long RM_TIMER_PERIOD = 2000;

        [XmlIgnore]
        private long start_t { get; set; }      //Marker for start of rotor run
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

        #region LOGGING

        public string path { get; set; }
        
        //Stores the start time (motor logs are recorded relative)
        public void RotorLogStart()
        {
            doWrite("-------------------------------------------\r\nt\tTarget\tActual\tP\tI\tD");
            start_t = LoggerState.GetTime_ms;
        }
        
        public void LogWrite()
        {

            long millisecs = LoggerState.GetTime_ms - start_t;

            string str = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                millisecs,
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
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(testpath + @"\rotor.log", true))
            {
                file.WriteLine(str);
            }
        }

        #endregion

        public float tolerance { get; set; } = 1.1f;
        //public long stableperiod { get; set; } = 3000;

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

        //NOTE: x is in timer ticks (not seconds)
        [XmlIgnore]
        private int x = 0;
        [XmlIgnore]
        [MotorProperty]
        public float Lower { get; set; }
        [XmlIgnore]
        [MotorProperty]
        public float Upper { get; set; }
        [XmlIgnore]
        public float graphrange = 50;        
        
        [XmlIgnore]
        public bool IsRotorInRange {get { return (rotor_speed > Lower && rotor_speed < Upper); }}

        [XmlIgnore]
        public bool IsReadyToSample { get{
                bool eval = EvalTrigger;
                Debug.WriteLine("Run time: " + (GetTime_ms - start_t).ToString() );
                return eval || (start_t != 0 && GetTime_ms - start_t > timeout);}
        }

        void SetBounds()
        {
            Lower = target_speed / tolerance;
            Upper = target_speed * tolerance;
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
                return (bool)_dt.Compute(smerged, "");
            }
        }

        #endregion
    }
}
