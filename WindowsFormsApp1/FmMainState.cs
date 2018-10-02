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
    enum APPSTATE { Ready, WaitRotor, TestRunning, Armed, TriggerWaitLock, DoSampling, Error };
    enum APPEVENT { InitRun, ACKRotor, Arm, Lock, Trigger, ContecTriggered, SamplingError, EndRun, Stop };

    public partial class FmMain: Form
    {
        //SamplingError:WaitTrigger

#region     AppState

        //Symbols

        StateMachine appstate = new StateMachine("Appstate", APPSTATE.Ready);

        //const int CONTECPOLLERSTATE = 1000;
        //const int CONTECPOLLERDATA = 100;

        void SetupAppStateMachine()
        {
            ///Entry point button
            appstate.AddRule(APPSTATE.Ready, APPEVENT.InitRun, APPSTATE.WaitRotor, InitRun);                //->Send Start Rotor
            appstate.AddRule(APPSTATE.WaitRotor, APPEVENT.ACKRotor, APPSTATE.TestRunning, NextRun);                                         //Start next run

            //Entry point device state
            appstate.AddRule(APPSTATE.TestRunning, APPEVENT.Arm, APPSTATE.Armed, (string index) =>
            {
                PrintLn("Armed", true);
                setStartButtonText(APPSTATE.Armed);
                //DOCS: Auto Trigger to by-pass motorcontol in testing
                //if (PersistentLoggerState.ps.data.testingmode)
                //    Task.Delay(5000).ContinueWith(t => myaio.TestTrigger() );
            });


            //TestRunning + Armed -> hand over to TriggerLogic
            TriggerLogic.AddEvents(appstate, rotorstate);

            //Wait 
            appstate.AddRule(null, APPEVENT.SamplingError, APPSTATE.WaitRotor, HandleSamplingError);
            appstate.AddRule(APPSTATE.DoSampling, APPEVENT.EndRun, APPSTATE.TestRunning, (string index) =>
            {
                SaveData();
                PrintLn("Saved", true);
                RunFinished();
                NextFreq(index);
            });

            appstate.AddRule(null, APPEVENT.Stop, APPSTATE.Ready, (string idx) =>
            {
                rotorstate.Event(ARDUINOEVENT.Send_Stop);
                rotorstate.Event(ARDUINOEVENT.Do_Stop);     //Incase ACK not returned
                StopSeries(idx);
            });

        }

        //Run before series
        void InitRun(string index)
        {
            if (lblTestPath.Text == "") { if (MessageBox.Show("Warning", "No filename set", MessageBoxButtons.OKCancel) == DialogResult.Cancel) { return; } }
            PrintLn("----------------------------------------------------\r\nApplied Settings");
            PrintLn(PersistentLoggerState.ps.ToString());

            //Start again (too heavy handed)
            //myaio.ResetDevices();
            //myaio.CheckDevices();

            PersistentLoggerState.ps.data.target_speed = float.Parse(cbxFreqFrom.SelectedValue.ToString());
            PersistentLoggerState.ps.data.rotor_speed = 0;

            //Start motor
            rotorstate.Event(ARDUINOEVENT.Send_Start);      //MotorControl -> Start -> Trigger -> LAX1664 (externally) - simulated by call to test device
            //Set rotor 0
            //HERE: Test0 must be before serialpoller start (else triggers: Test0 is 0 so timeouts!)
            PersistentLoggerState.ps.data.Rotor0 = PersistentLoggerState.ps.data.Test0 = 0;
            //Enters wait for ACK

            serialpoller.Start();       //Needed to look for serial ACK
            contecpoller.Start();

            dt.Clear();

            Task.Delay(5000).ContinueWith(t =>
            {
                if (PersistentLoggerState.ps.data.rotor_speed == 0) {
                    FmMain.ActiveForm.Invoke(new Action(() =>
                    {
                        appstate.Event(APPEVENT.Stop);
                        MessageBox.Show("Rotor is not running");
                        return;
                    }));
                }
            });
        }

        const int MAXROTORSPEEDFAILSAFE = 200;
        //At start each run
        void NextFreq(string index)
        {
            PersistentLoggerState.ps.data.target_speed += float.Parse(cbxFreqStep.SelectedValue.ToString());
            if (PersistentLoggerState.ps.data.target_speed > float.Parse(cbxFreqTo.SelectedValue.ToString())
                ||
                PersistentLoggerState.ps.data.target_speed > MAXROTORSPEEDFAILSAFE
                )
            {
                appstate.Event(APPEVENT.Stop);
                return;
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
            //Set T0 - 
            PersistentLoggerState.ps.data.Test0 = 0;
            myaio.Start();
        }

        void RunFinished()
        {
            myaio.Stop();
            myaio.RefreshDevices();
            myaio.ResetRediscoverDevices();
            trigger.Reset();
            rotorstate.Event(ARDUINOEVENT.Next);              //Returns the arduino state to running
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
            RunFinished();
            serialpoller.Stop();
            SetStatus("Ready");
            PrintLn("Run Stopped", true);

        }
        #endregion

#region ROTOR CONTROL

        void InitMotorStateMachine()
        {
            rotorstate.AddRule(StateMachine.OR(ARDUINOSTATE.Ready, ARDUINOSTATE.Triggered), ARDUINOEVENT.Send_Start, (string idx) =>
            {
                //Start
                //TODO: should wait for freq?
                //SendCommand(CMD.SETFREQ);
                //SendCommand(CMD.GETTARGETFREQ);
                SendCommand(CMD.START);
            });

            //TODO: can remove Callback
            rotorstate.AddRule(ARDUINOSTATE.Lockable, ARDUINOEVENT.Send_Start, SEND_LOCK);
            rotorstate.AddRule(ARDUINOSTATE.Locked, ARDUINOEVENT.Send_Start, SEND_UNLOCK);
            rotorstate.AddRule(ARDUINOSTATE.Lockable, ARDUINOEVENT.Send_Lock, SEND_LOCK);
            rotorstate.AddRule(ARDUINOSTATE.Locked, ARDUINOEVENT.Send_Unlock, SEND_UNLOCK);

            rotorstate.AddRule(null, ARDUINOEVENT.Send_Stop, (string idx) => { SendCommand(CMD.STOP); });
            rotorstate.AddRule(StateMachine.OR(ARDUINOSTATE.Running, ARDUINOSTATE.Lockable, ARDUINOSTATE.Locked), ARDUINOEVENT.Send_Trigger, (string idx) =>
            {
                //Sync Frequency
                SendCommand(CMD.SETADC);
                SendCommand(CMD.GETADC);
                //Send Trigger
                myaio.InitDataCollectionTimeout();
                SendCommand(CMD.TRIGGER);
            });

            //These events match any state (series shouldn't be returning events to wrong state)
            //Automatically called by returning ACKs 
            rotorstate.AddRule(null, ARDUINOEVENT.Do_Start, ARDUINOSTATE.Running, ACK_START);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_Stop, ARDUINOSTATE.Ready, ACK_STOP);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_Lock, ARDUINOSTATE.Locked, ACK_LOCK);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_unlock, ARDUINOSTATE.Lockable, ACK_UNLOCK);
            //sm_motor.AddRule(null, ARDUINOEVENT.Do_SetPulseDelay, ACK_SETPULSEDELAY);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_SetPID, ACK_SETPID);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_SetFreq, ACK_SETFREQ);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_Trigger, ARDUINOSTATE.Triggered, ACK_TRIGGER);
            rotorstate.AddRule(null, ARDUINOEVENT.Do_SetADC, ACK_SETADC);
            rotorstate.AddRule(ARDUINOSTATE.Triggered, ARDUINOEVENT.Next, ARDUINOSTATE.Running);
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
            setStartButtonText(APPSTATE.TestRunning);
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
            setStartButtonText(0);
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
            //if (PersistentLoggerState.ps.data.testingmode != 0)
            //{
                //Simulate a trigger in the LAX1664
                myaio.SimulateTrigger();
            //}
#endif
        }

        void ACK_SETADC(string idx)
        {
            AsyncText(toolStripStatusLabel1, "ADC set.");
        }


        #endregion








    }
}
