using MotorController;
using MultiDeviceAIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorController
{
    /// <summary>
    /// There will be several attempts to get the trigger right
    /// (1) over sample and trim
    /// (2) lock, mod, unlock, ->
    /// etc
    /// 
    /// so needs control of motor sm, app sm
    /// needs to send the trigger signal
    /// needs stats
    /// 
    /// </summary>
    public class TriggerLogic
    {
        public MovingStatsCrosses mac { get; set; }
        StateMachine appsm, rsm;
        LoggerState state;
        DataTable dt = new DataTable();     //For Compute

        public TriggerLogic(MovingStatsCrosses mac, StateMachine appsm, StateMachine rsm, LoggerState state)
        {
            this.mac = mac;      //holds a reference to target_freq
            this.appsm = appsm;
            this.rsm = rsm;
            this.state = state;  //state holds this object
        }

        public void Reset()
        {
            mac.Reset();
        }

        public void AddRotorSpeed(float speed)
        {
            
            mac.Add(speed);

            Debug.WriteLine("add: ma=" + mac.MA + ",sd=" + mac.STD);
        }

        public bool IsReadyToSample
        {
            get
            {
                if (!mac.bufferfull) return false;                  //not reliable stats if only processes a few rotors. Means also the rotor must conform over the period to fill buffer.
                if (state.TestX > state.timeout) return true;       //timeout
                return EvalTrigger;
                //Debug.WriteLine("TEval: " + eval);
                //Debug.WriteLine("TX>TO: " + state.TestX + ">" + state.timeout);
            }
        }

        #region EVAL TRIGGER 

        public string TriggerMerged
        {
            get
            {
                string mergestring = LoggerState.MergeObjectToString<MotorPropertyAttribute>(mac, state.metriccommand);
                return mergestring;
            }
        }

        public bool EvalTrigger
        {
            get
            {
                string smerged = TriggerMerged;

                Debug.WriteLine("Merged: " + smerged);

                return (bool)dt.Compute(smerged, "");
            }
        }

        #endregion


    }
}
