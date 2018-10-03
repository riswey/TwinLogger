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
        LoggerState state;
        DataTable dt = new DataTable();     //For Compute

        public TriggerLogic(MovingStatsCrosses mac, LoggerState state)
        {
            this.mac = mac;      //holds a reference to target_freq
            this.state = state;  //state holds this object
        }

        //Rotor:Running + App:Armed -> hand over to TriggerLogic
        //Finish by DoSampling
        static public void AddEvents(StateMachine smapp, StateMachine smrotor)
        {
            //TODO: need to timeout events (send event - stored by marshal and alternative executed if timeout (retry)
            //Removed TriggerWaitLock (not important if lock doesn't take)
            //appstate.AddRule(APPSTATE.Armed, APPEVENT.Trigger, APPSTATE.TriggerWaitLock, (string index) =>
            
            //TODO: This is wrong. Send_trigger should set the appstate to DoSampling!
            smapp.AddRule(APPSTATE.Armed, APPEVENT.Trigger, APPSTATE.DoSampling, (string index) =>
            {
                smrotor.Event(ARDUINOEVENT.Send_Lock);
                smrotor.Event(ARDUINOEVENT.Send_Trigger);
            });
            /*
            appstate.AddRule(APPSTATE.TriggerWaitLock, APPEVENT.Lock, APPSTATE.DoSampling, (string index) =>
            {
                sm_motor.Event(ARDUINOEVENT.Send_Trigger);
                //TODO: check that contecpolling_Tick starts polling now (when sampling)
            });
            */
            /*
            appstate.AddRule(APPSTATE.Sampling, APPEVENT.ContecTriggered, (string index) =>
            {
                //Confirm Contec
                //contecpoller.Interval = CONTECPOLLERDATA;
                PrintLn("Triggered", true);
                setStartButtonText(2);
            });
            */

        }

        public void Reset()
        {
            mac.Reset();
        }

        #region EVAL TRIGGER 

        //TriggerLogic exposes the change here. Client does the actual trigger
        public bool IsReadyToSample
        {
            get
            {
                if (!mac.bufferfull) return false;                  //not reliable stats if only processes a few rotors. Means also the rotor must conform over the period to fill buffer.
                if (state.TestX > state.timeout) return true;       //timeout
                return EvalTrigger;
            }
        }


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
            //Need this to fail well. However encode the failure state for the correct client.
            get
            {
                string smerged = TriggerMerged;
                return (bool) dt.Compute(smerged, "");
            }
        }

        #endregion


    }
}
