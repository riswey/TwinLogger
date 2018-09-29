using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotorController;

namespace MultiDeviceAIO
{
    public partial class FmControlPanel: Form
    {
        //SamplingError:WaitTrigger

#region     AppState

        //Symbols
        enum APPSTATE { Ready, WaitRotor, TestRunning, Armed, TriggerWaitLock, DoSampling, Error };
        enum APPEVENT { InitRun, ACKRotor, Armed, Lock, Trigger, ContecTriggered, SamplingError, EndRun, Stop };

        StateMachine appstate = new StateMachine("Appstate", APPSTATE.Ready);

        //const int CONTECPOLLERSTATE = 1000;
        //const int CONTECPOLLERDATA = 100;

        void SetupAppStateMachine()
        {
            ///Entry point button
            appstate.AddRule(APPSTATE.Ready, APPEVENT.InitRun, APPSTATE.WaitRotor, InitRun);                //->Send Start Rotor
            appstate.AddRule(APPSTATE.WaitRotor, APPEVENT.ACKRotor, APPSTATE.TestRunning, NextRun);                                         //Start next run

            //Entry point device state
            appstate.AddRule(APPSTATE.TestRunning, APPEVENT.Armed, APPSTATE.Armed, (string index) =>
            {
                PrintLn("Armed", true);
                setStartButtonText(1);
                //DOCS: Auto Trigger to by-pass motorcontol in testing
                //if (PersistentLoggerState.ps.data.testingmode)
                //    Task.Delay(5000).ContinueWith(t => myaio.TestTrigger() );
            });
            
            //TODO: need to timeout events (send event - stored by marshal and alternative executed if timeout (retry)
            //Removed TriggerWaitLock (not important if lock doesn't take)
            //appstate.AddRule(APPSTATE.Armed, APPEVENT.Trigger, APPSTATE.TriggerWaitLock, (string index) =>
            appstate.AddRule(APPSTATE.Armed, APPEVENT.Trigger, APPSTATE.DoSampling, (string index) =>
            {
                sm_motor.Event(ARDUINOEVENT.Send_Lock);
                sm_motor.Event(ARDUINOEVENT.Send_Trigger);
                myaio.InitDataCollectionTimeout();
                PersistentLoggerState.ps.data.Test0 = 0;
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
            //Wait 
            appstate.AddRule(null, APPEVENT.SamplingError, APPSTATE.WaitRotor, HandleSamplingError);
            appstate.AddRule(APPSTATE.DoSampling, APPEVENT.EndRun, APPSTATE.TestRunning, (string index) =>
            {
                SaveData();
                PrintLn("Saved", true);
                RunFinished();
                NextFreq(index);
            });
            appstate.AddRule(null, APPEVENT.Stop, APPSTATE.Ready, StopSeries);
            appstate.AddRule(APPSTATE.TestRunning, APPEVENT.Stop, APPSTATE.Ready, StopSeries);

        }

        //Run before series
        void InitRun(string index)
        {
            if (tbDirectory.Text == "") { if (MessageBox.Show("Warning", "No filename set", MessageBoxButtons.OKCancel) == DialogResult.Cancel) { return; } }
            PrintLn("----------------------------------------------------\r\nApplied Settings");
            PrintLn(PersistentLoggerState.ps.ToString());

            //Start again (too heavy handed)
            //myaio.ResetDevices();
            //myaio.CheckDevices();

            PersistentLoggerState.ps.data.target_speed = (float)nudFreqFrom.Value;

            //Start motor
            sm_motor.Event(ARDUINOEVENT.Send_Start);      //MotorControl -> Start -> Trigger -> LAX1664 (externally) - simulated by call to test device
            //Set rotor 0
            PersistentLoggerState.ps.data.Rotor0 = 0;
            //Enters wait for ACK

            //TODO: better timer control so that exiting closures ensures they stop/start
            //STOP Monitor Timer
            serialpoller.Start();       //Needed to look for serial ACK
            contecpoller.Start();
        }

        const int MAXROTORSPEEDFAILSAFE = 200;
        //At start each run
        void NextFreq(string index)
        {
            PersistentLoggerState.ps.data.target_speed += (float)nudFreqStep.Value;
            if (PersistentLoggerState.ps.data.target_speed > (float)nudFreqTo.Value
                ||
                PersistentLoggerState.ps.data.target_speed > MAXROTORSPEEDFAILSAFE
                )
            {
                appstate.Event(APPEVENT.Stop);
            }

            //TODO: Also sets start_t!!! Need to formalise this its to important to be a side effect!
            
            NextRun(index);
        }

        void NextRun(string index)
        {
            //TODO: should be bound more closely to change freq state. But only called once so here.
            SendCommand(CMD.SETFREQ);       //Inform Arduino
            SendCommand(CMD.GETTARGETFREQ);
            PrintLn("Target frequency " + PersistentLoggerState.ps.data.target_speed, true);
            //TODO: wait on getfreq!
            //TODO: this should be on Freq ACK! Use GetFreq data
            UpdateChartYScale();            //DOCS: chart range is auto_updated when data.target_speed changed
            myaio.RefreshDevices();
            //var num_samples = nudDuration.Value * (decimal)1E6 / nudInterval.Value;
            myaio.SetupTimedSample(PersistentLoggerState.ps.data);
            PrintLn("Start", true);
            PersistentLoggerState.ps.data.RotorLogStart();
            myaio.Start();
        }

        void RunFinished()
        {
            myaio.Stop();
            myaio.RefreshDevices();
            myaio.ResetDevices();
            PersistentLoggerState.ps.data.ResetMAC();
            sm_motor.Event(ARDUINOEVENT.Next);
            pbr0.Value = 0;
            pbr1.Value = 0;
            setStartButtonText(0);
        }

        void HandleSamplingError(string index)
        {
            PrintLn(index);
            PrintLn("Error. Reset.", true);
            //Ignore unless sampling -> bad sample -> stop devices, reset data, set up again
            RunFinished();
            //myaio.ResetDevices();
            //contecpoller.Start();
            //Event which calls NextTest
            appstate.Event(APPEVENT.ACKRotor);
        }

        void StopSeries(string index)
        {
            sm_motor.Event(ARDUINOEVENT.Send_Stop);
            RunFinished();
            serialpoller.Stop();
            SetStatus("Ready");
            PrintLn("Run Stopped", true);
        }

        #endregion



        #region ROTOR CONTROL

        void InitMotorStateMachine()
        {
            sm_motor.AddRule(StateMachine.OR(ARDUINOSTATE.Ready, ARDUINOSTATE.Triggered), ARDUINOEVENT.Send_Start, (string idx) =>
            {
                //Start
                //TODO: should wait for freq?
                //SendCommand(CMD.SETFREQ);
                //SendCommand(CMD.GETTARGETFREQ);
                SendCommand(CMD.START);
            });

            //TODO: can remove Callback
            sm_motor.AddRule(ARDUINOSTATE.Lockable, ARDUINOEVENT.Send_Start, SEND_LOCK);
            sm_motor.AddRule(ARDUINOSTATE.Locked, ARDUINOEVENT.Send_Start, SEND_UNLOCK);
            sm_motor.AddRule(ARDUINOSTATE.Lockable, ARDUINOEVENT.Send_Lock, SEND_LOCK);
            sm_motor.AddRule(ARDUINOSTATE.Locked, ARDUINOEVENT.Send_Unlock, SEND_UNLOCK);

            sm_motor.AddRule(null, ARDUINOEVENT.Send_Stop, (string idx) => { SendCommand(CMD.STOP); });
            sm_motor.AddRule(StateMachine.OR(ARDUINOSTATE.Running, ARDUINOSTATE.Lockable, ARDUINOSTATE.Locked), ARDUINOEVENT.Send_Trigger, (string idx) =>
            {
                //Send Trigger
                SendCommand(CMD.SETADC);
                SendCommand(CMD.GETADC);
                SendCommand(CMD.TRIGGER);
            });

            //These events match any state (series shouldn't be returning events to wrong state)
            sm_motor.AddRule(null, ARDUINOEVENT.Do_Start, ARDUINOSTATE.Running, ACK_START);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_Stop, ARDUINOSTATE.Ready, ACK_STOP);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_Lock, ARDUINOSTATE.Locked, ACK_LOCK);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_unlock, ARDUINOSTATE.Lockable, ACK_UNLOCK);
            //sm_motor.AddRule(null, ARDUINOEVENT.Do_SetPulseDelay, ACK_SETPULSEDELAY);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_SetPID, ACK_SETPID);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_SetFreq, ACK_SETFREQ);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_Trigger, ARDUINOSTATE.Triggered, ACK_TRIGGER);
            sm_motor.AddRule(null, ARDUINOEVENT.Do_SetADC, ACK_SETADC);
            sm_motor.AddRule(ARDUINOSTATE.Triggered, ARDUINOEVENT.Next, ARDUINOSTATE.Running);
        }


        void SEND_LOCK(string idx)
        {
            SendCommand(CMD.SETLOCK);
        }

        void SEND_UNLOCK(string idx)
        {
            SendCommand(CMD.SETUNLOCK);
        }

        void ACK_START(string idx)
        {
            //PersistentLoggerState.ps.data.Start();   //Removed Motor Control Logging
            //PersistentLoggerState.ps.data.StartRMTimer();
            setStartButtonText(3);
            //TODO: need check if lockable before lock when sampling!!!!
            //Task task = Task.Delay(5000).ContinueWith(t => ProcessEvent(EVENT.Lock));
            //this.Invoke(new Action(() => {
            appstate.Event(APPEVENT.ACKRotor);
            //}));
        }

        //TODO: Need to move all this btnStart stuffinto main app

        void ACK_STOP(string idx)
        {
            //AsyncDisable(this.btnSetSpeed, false);
            AsyncColor(btnStart, default(Color));
            AsyncText(btnStart, "Start");
        }

        void ACK_LOCK(string idx)
        {
            appstate.Event(APPEVENT.Lock);
            AsyncText(btnStart, "Unlock");
            AsyncColor(btnStart, Color.Red);
        }

        void ACK_UNLOCK(string idx)
        {
            AsyncText(btnStart, "Lock");
            AsyncColor(btnStart, Color.Orange);
            //AsyncDisable(this.btnSetSpeed, false);
        }

        void ACK_SETPULSEDELAY(string idx)
        {
            AsyncText(toolStripStatusLabel1, "Pulse Delay set.");
        }

        void ACK_SETPID(string idx)
        {
            AsyncText(toolStripStatusLabel1, "PID set.");
        }
        void ACK_SETFREQ(string idx)
        {
            //TODO: Should SETFREQ -> StartRMTImer ???
            //PersistentLoggerState.ps.data.StartRMTimer();
            AsyncText(toolStripStatusLabel1, "Target Rotor Frequency set.");
        }

        void ACK_TRIGGER(string idx)
        {
#if SOFTDEVICE
            if (PersistentLoggerState.ps.data.testingmode != 0)
            {
                //Simulate a trigger in the LAX1664
                myaio.SimulateTrigger();
            }
#endif
        }

        void ACK_SETADC(string idx)
        {
            AsyncText(toolStripStatusLabel1, "ADC set.");
        }


        #endregion








    }
}
