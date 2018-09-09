using System;
using System.Windows.Forms;
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;

namespace MultiDeviceAIO
{

    public partial class Main : Form
    {

        void StartSampling()
        {
            if (PersistentLoggerState.ps.data.n_devices != 2)
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

            ////////////////////////////////////////////////////////////////////
            // RESET DATA HERE
            ////////////////////////////////////////////////////////////////////
            myaio.ResetTest();

            var num_samples = nudDuration.Value * (decimal)1E6 / nudInterval.Value;

            myaio.SetupTimedSample(PersistentLoggerState.ps.data);

            //pfunc = Marshal.GetFunctionPointerForDelegate(pdelegate_func);
            //myaio.SetAiCallBackProc(pfunc);


            //STOP TIMER
            TimerMonitorState(false);

            PrintLn("Start");

            myaio.Start();
            timergetdata.Start();
        }

        private void data_Tick(object sender, EventArgs e)
        {

            //TODO: improve this state logic
            //check start sampling has reset everything
            //don't pass data around by value
            //

            //Can only do 1 thing at a time.

            //PrintLn(myaio.state + ",", 0);

            int status = 0;
            //Must not request if running.
            //So once armed wait for callback and block everything else

            status = myaio.GetStatusAll();

            PrintLn(status.ToString("X") + ",", 0);

            //Respond to status
            //Overflow
            if ((status & (int)CaioConst.AIS_OFERR) == (int)CaioConst.AIS_OFERR)
            {
                PrintLn("Device Overflow");
                Abort();
                return;
            }
            //Timer error
            if ((status & (int)CaioConst.AIS_SCERR) == (int)CaioConst.AIS_SCERR)
            {
                PrintLn("Sampling Clock Error");
                //Abort();
                //return;
            }

            //Waiting for trigger
            if ((status & (int)CaioConst.AIS_START_TRG) == (int)CaioConst.AIS_START_TRG)
            {
                //Its waiting
            }

            //Its collecting data
            if ((status & (int)CaioConst.AIS_DATA_NUM) == (int)CaioConst.AIS_DATA_NUM)
            {
                setStartButtonText(true);
                RetrieveData();
                //Invoke(new Action(() => RetrieveData()));
            }

        }

        private void RetrieveData()
        {
            double percent = myaio.RetrieveAllData() / myaio.testtarget * 100;

            PrintLn(String.Format("{0:0.00}%", percent), -1);

            //Take a few more samples to get the 0s that end the test 
            if (percent == 100.0)
            {
                PrintLn("Finishing...", 0);
            }

            if (myaio.IsTestFinished)
            {
                timergetdata.Stop();
                TerminateTest();
            }
        }

        void TerminateTest()
        {
            if (myaio.IsTestFailed)
            {
                PrintLn("Failed");
                PrintLn(myaio.GetStatusAll());
                StartSampling();
                return;
            }
            //Success
            TestFinished();
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
        void TestFinished()
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

            setStartButtonText(false);

            //RESTART TIMER
            TimerMonitorState(true);

            SetStatus("Ready");

        }

    }
}
