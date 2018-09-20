using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;

namespace MultiDeviceAIO
{

    public partial class FmControlPanel : Form
    {
        
        void StartSampling()
        {
            //TODO: for twin this should be 2
            if (PersistentLoggerState.ps.data.n_devices == 0)
            {
                SetStatus("Error: Incorrect Device number. Reset");
                return;
            }

            //Start again (too heavy handed)
            //myaio.ResetDevices();

            //TODO: no device check
            /*
            List<int> failedID;
            if (!myaio.DeviceCheck())
            {
                
                string status = "Error: Devices not responding. Device(s) ";
                foreach (short f in failedID)
                {
                    status += myaio.devicenames[f] + " ";
                }

                status += "failed.";
                
                //SetStatus(status);

                //?Who failed
                PrintLn("Failed");
                return;
            }
            */

            var status = new List<int>();
            myaio.GetStatusAll(ref status);

            ////////////////////////////////////////////////////////////////////
            // RESET DATA HERE
            ////////////////////////////////////////////////////////////////////
            myaio.ResetTest();

            var num_samples = nudDuration.Value * (decimal)1E6 / nudInterval.Value;

            myaio.SetupTimedSample(PersistentLoggerState.ps.data);

            //pfunc = Marshal.GetFunctionPointerForDelegate(pdelegate_func);
            //myaio.SetAiCallBackProc(pfunc);


            //TODO: better timer control so that exiting closures ensures they stop/start
            //STOP Monitor Timer
            TimerMonitorState(false);

            PrintLn("Start", true);

            myaio.Start();

            timergetdata.Start();
        }

        private void data_Tick(object sender, EventArgs e)
        {
            //This the data collection driver
            InteractWithDevices();
        }

        private void InteractWithDevices()
        {
            var status = new List<int>();

            myaio.SampleDeviceState(ref status, out int allstatus, out MyAIO.DEVICESTATEDELTA delta);

            if (PersistentLoggerState.ps.data.testingmode == 1)
            {
                //waiting for trigger
                if (allstatus == 2)
                {
                    //TODO: we havge no trigger in testing mode 1 so do in software
                }
            }

            //Draw status
            DrawStatusStrip(status.ToArray());

            //Handle delta
            if (delta == MyAIO.DEVICESTATEDELTA.ARMED)
            {
                //A device just got armed
                PrintLn("Armed", true);
                setStartButtonText(1);
            }

            if (delta == MyAIO.DEVICESTATEDELTA.SAMPLING)
            {
                //A device just got triggered
                PrintLn("Sampling", true);
                setStartButtonText(2);
            }

            //Handle bitflags

            //A device got Overflow error
            if (MyAIO.TestBit(allstatus, CaioConst.AIS_OFERR))
            {
                PrintLn("Device Overflow", true);
                Abort();
                StartSampling();
                return;
            }

            //A device got Clock error
            if (MyAIO.TestBit(allstatus, CaioConst.AIS_SCERR))
            {
                PrintLn("Sampling Clock Error", true);
                Abort();
                StartSampling();
                return;
            }

            //Collecting data || just got an Idle status after sampling
            if (
                MyAIO.TestBit(allstatus, CaioConst.AIS_DATA_NUM)
                || 
                delta == MyAIO.DEVICESTATEDELTA.READY )         //empty buffers
            {
                //TODO: May as well do one main thread
                //but exception handling gets stuck here

                //NOTE: accessviolation
                //Invoke(new Action(() => RetrieveData()));

                RetrieveData();

                if (myaio.IsTestFinished)
                {
                    FinishTest();
                }

            }

        }

        void FinishTest()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => FinishTest() ));
                return;
            }

            timergetdata.Stop();
            if (myaio.IsTestFailed)
            {
                PrintLn("Test Failed", true);
                //PrintLn(myaio.GetStatusAll());
                StartSampling();
                return;
            }
            //Success
            SaveData();

            setStartButtonText(0);
            //RESTART TIMER
            TimerMonitorState(true);
            SetStatus("Ready");
        }

        void ResetUSB()
        {
            MessageBox.Show("Not Implemented");
        }

        private void RetrieveData()
        {
            double percent = myaio.RetrieveAllData() / myaio.testtarget * 100;

            progressBar1.Value = (int)Math.Round(percent, 0);

            PrintLn(String.Format("{0:0.00}%", percent), false, -1);
            
        }

        /// <summary>
        /// Secure data to disk
        /// Get User input (freq, filename)
        /// Cancel or
        /// Header + Data to newfile
        /// Cleanup
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="num_samples"></param>
        void SaveData()
        {
            DATA concatdata = myaio.GetConcatData;

            //Get new temp and add update AIOSettings.singleInstance
            string filepath = IO.GetFilePathTemp(PersistentLoggerState.ps.data);

            filepath = IO.CheckPath(filepath, false);

            IO.SaveDATA(PersistentLoggerState.ps.data, ref filepath, concatdata);

            /*
            //Generate User reports to see what happened
            PrintLn("+---Report-----------------------------------------");
            foreach(KeyValuePair<DEVICEID, List<int>> device_data in concatdata)
            {
                PrintLn("| Device: " + myaio.devicenames[device_data.Key] + " (" + device_data.Value.Count + ")");
                PrintLn(GenerateDataReport(device_data.Value));
            }
            PrintLn("+--------------------------------------------------");
            */
            //SAVE LOG
            SaveLogFile();

            //SetStatus("Awaiting User Input...");

            //Now ask user input to save
            //Provide a freq -> complete header. Save header
            //string fn = UserInputAfterSampling();

            string fn;
            //get filename
            if (checkBox1.Checked)
            {
                fn = IO.GetFilePathCal(PersistentLoggerState.ps.data, cbOrientation.SelectedIndex);
            }
            else
            {
                //beware _1 is the file rename index
                string DATAFILEFORMAT = PersistentLoggerState.ps.data.datafileformat;
                string extension = "jdd";
                //awesome: this is recursive if you want :-)
                fn = IO.GetFilePathTest(PersistentLoggerState.ps.data, DATAFILEFORMAT, extension);
                fn = IO.CheckPath(fn, false);
            }

            RenameTempFile(fn);
            PrintLn("Saved", true);

        }

    }
}
