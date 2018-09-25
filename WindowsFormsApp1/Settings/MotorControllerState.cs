using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MultiDeviceAIO
{
    public class MotorPropertyAttribute : Attribute { }

    public partial class LoggerState
    {

        void InitMotorControllerState()
        {
            mac = new MotorController.MovingStatsCrosses(metric_window, () => { return target_speed; } );
        }

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

        float _target_speed = 50;
        public float target_speed
        {
            get
            {
                return _target_speed;
            }
            set
            {
                _target_speed = value;
                SetBounds();
            }
        }

        #region ROTOR METRIC WINDOW

        //TODO: make moving average period dynamic
        //TODO: remove crosses/min/max from trailing window boundary

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
        
        float _rotor_speed = 0;
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
            }
        }

        /*
         * Metrics used to create rotor constraints
         * UPPERTARGETBOUNDARY
         * LOWERTARGETBOUNDARY
         * TARGET
         * Current: min_v_target > LOWERTARGETBOUNDARY, max_v_target < UPPERTARGETBOUNDARY
         * Also
         * MA (within much tighter bounds of target)
         * MSTD (< TARGETBOUNDARY)
         * CROSSES (> n) more crosses -> better stability
         * ?? Daves Min/Max + Pulse
         * 
         */

        #endregion

        #region MinMax_TIMER
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
            //doWrite("-------------------------------------------\r\nt\tTarget\tActual\tP\tI\tD\tDelay\tMin\tMax");
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

        public float tolerance { get; set; } = 1.1f;
        public long stableperiod { get; set; } = 3000;

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
        public long enterrange = 0;

        //TODO: really questionable whether enterrange state is a parameter state or a machine state!
        public void ResetMotorWindow()
        {
            enterrange = 0;
        }

        [XmlIgnore]
        public bool IsRotorInRange
        {
            get
            {
                if (rotor_speed > Lower && rotor_speed < Upper)
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


        /*
         * TODO: This is being replaced with metric window
         * 
         * 
         * 
         */

        [XmlIgnore]
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

        [XmlIgnore]
        public bool IsReadyToSample
        {
            get
            {
                Debug.WriteLine("Is stable: " + IsRotorStable);
                Debug.WriteLine("merged: " + TriggerMerged);
                //{UPPER} - {MAX} >0 AND {LOWER} - {MIN} < 0
                

                bool eval = EvalTrigger;

                Debug.WriteLine(this.Upper + "," + this.Lower + "," + Max + "," + Min + "(" + eval + ")");
                return eval;

                //DOC: Samples after timeout whatever. This is the window for improved motor control
                //return IsRotorStable;// || (start_t != 0 && GetTime() - start_t > timeout);
            }
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
                return (bool)_dt.Compute(TriggerMerged, "");
            }
        }

        #endregion



        }
}
