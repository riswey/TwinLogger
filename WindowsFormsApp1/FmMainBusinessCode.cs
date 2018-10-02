using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;

namespace MultiDeviceAIO
{

    public partial class FmMain : Form
    {
        static long[] lasterrorcount = new long[2];

        protected void contecpoller_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("###### " + lasterrorcount[1]);

            if (lasterrorcount[1] > 5)
            {
                //Fail this
                contecpoller.Stop();        //TODO: why doesn't this stop in EVENT.Stop?
                appstate.Event(APPEVENT.Stop);
                MessageBox.Show(this, "Device unstable. Trigger interference. Terminating series.", "Contec Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                lasterrorcount[1] = 0;
                return;
            }

            //Data Collection Main
            AsyncText(lblAppState, appstate.state.ToString());
            AsyncText(lblRotorState, rotorstate.state.ToString());

            //DOC: Once entered sampling state keep going until got expected or timeout.
            //Don't hassle devices with any other requests
            if (appstate.IsState(APPSTATE.DoSampling))
            {
                if (DoSampling())
                {
                    //Sampling finished
                    //CRITICAL. Without change of state will hang here!
                    appstate.Event(APPEVENT.EndRun);
                }
                return;
            }

            //set status list
            List<int> status = new List<int>();
            myaio.GetStatusAll(ref status);

            //Draw status
            DrawContecStatusStrip(status.ToArray());

            //Process Events

            int bitflags = 0;
            foreach (int s1 in status)
            {
                bitflags |= s1;
            }

            Debug.WriteLine("Bit: " + bitflags);

            //Do count of frequent errors
            long now = LoggerState.GetTime_ms;

            if (bitflags > 65535)
            {
                if (now - lasterrorcount[0] < 15000)  //0x10000+
                {
                    lasterrorcount[1]++;
                }
                else
                {
                    lasterrorcount[1] = 0;
                }
                lasterrorcount[0] = now;
            }

            if (bitflags > 65535)
            {
                appstate.Event(APPEVENT.SamplingError);
                return;
            }

            //0 means idling
            if (((int)CaioConst.AIS_START_TRG & bitflags) != 0)
            {
                //Armed
                if (!appstate.IsState(APPSTATE.Armed)) appstate.Event(APPEVENT.Arm);
            }

            if (((int)CaioConst.AIS_DATA_NUM & bitflags) != 0)
            {
                //Got data in buffer
                appstate.Event(APPEVENT.ContecTriggered);
            }

            //Device errors
            //Keep this fine as they can be treated differently!
            /*
            if (((int)CaioConst.AIS_OFERR & bitflags) != 0)
            {
                //Overflow
                appstate.Event(APPEVENT.SamplingError);
            }

            if (((int)CaioConst.AIS_SCERR & bitflags) != 0)
            {
                //Clock error
                appstate.Event(APPEVENT.SamplingError);
            }

            if (((int)CaioConst.AIS_DRVERR & bitflags) != 0)
            {
                //Driver
                appstate.Event(APPEVENT.SamplingError);
            }

            if (((int)CaioConst.AIS_AIERR & bitflags) != 0)
            {
                //Conversion error
                appstate.Event(APPEVENT.SamplingError);
            }
            */
        }


        bool DoSampling()
        {
            RetrieveData();

            //NOTE: if finished then timeout not important
            if (myaio.IsTestFinished)
            {
                /*
                if (myaio.IsTestFailed)
                {
                    appstate.Event(APPEVENT.SamplingError);
                    return false;
                    //throw new TTISamplingFailure();
                }
                */
                return true;
            }
            else if (myaio.IsTimeout)
            {
                appstate.Event(APPEVENT.SamplingError);
                return false;
            }
            else
            {
                //wait for some more data
                return false;
            }

        }


        void ResetUSB()
        {
            MessageBox.Show("Not Implemented");
        }

        private void RetrieveData()
        {
            myaio.RetrieveAllData(out List<int> progress);

            pbr0.Value = (int)Math.Round(progress[0] / myaio.devicetarget * 100, 0);
            pbr1.Value = (int)Math.Round(progress[1] / myaio.devicetarget * 100, 0);

            PrintLn(String.Format("A:{0:0.00}% B:{0:0.00}%", pbr0.Value, pbr1.Value), false, -1);
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
            myaio.ConcatDataFromDevices();

            //Get new temp and add update AIOSettings.singleInstance
            string filepath = IO.GetFilePathTemp(PersistentLoggerState.ps.data);

            filepath = IO.PreparePath(filepath, false);

            IO.SaveDATA(PersistentLoggerState.ps.data, ref filepath, myaio.ConcatData);

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
            fmlog.SaveLogFile();

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
                fn = IO.PreparePath(fn, false);
            }

            RenameTempFile(fn);

        }

    }
}
