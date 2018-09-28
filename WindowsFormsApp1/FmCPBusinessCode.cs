using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;

namespace MultiDeviceAIO
{
    /*
    class ContecClockSignalError: Exception {
        public ContecClockSignalError(){}
        public ContecClockSignalError(string message): base(message){}
        public ContecClockSignalError(string message, Exception inner): base(message, inner){}
    }

    class ContecDeviceBufferOverflow: Exception
    {
        public ContecDeviceBufferOverflow() { }
        public ContecDeviceBufferOverflow(string message) : base(message) { }
        public ContecDeviceBufferOverflow(string message, Exception inner) : base(message, inner) { }
    }

    class TTISamplingFailure : Exception
    {
        public TTISamplingFailure() { }
        public TTISamplingFailure(string message) : base(message) { }
        public TTISamplingFailure(string message, Exception inner) : base(message, inner) { }
    }
    */

    public partial class FmControlPanel : Form
    {
        
        protected void contecpoller_Tick(object sender, EventArgs e)
        {
            //This the data collection driver
            //try
            //{
            AsyncText(lblAppState, appstate.state.ToString());
            AsyncText(lblRotorState, sm_motor.state.ToString());

            if ( ProcessDrawContecState() )
                {
                    Debug.WriteLine("Test End A: " + appstate.state);
                    appstate.Event(APPEVENT.EndRun);
                    Debug.WriteLine("Test End B: " + appstate.state);
                return;
                }
            /*}
            catch (Exception ex) {
                if ( ex is ContecDeviceBufferOverflow || ex is ContecClockSignalError || ex is ContecClockSignalError || ex is TTISamplingFailure)
                {
                    appstate.Event(APPEVENT.SamplingError);
                    return;
                }
                throw;
            }*/
        }

        private bool ProcessDrawContecState()
        {
            var status = new List<int>();

            myaio.SampleDeviceState(ref status, out int allstatus, out MyAIO.DEVICESTATEDELTA delta);

            //Draw status
            DrawContecStatusStrip(status.ToArray());

            //Handle delta
            if (delta == MyAIO.DEVICESTATEDELTA.ARMED)
            {
                appstate.Event(APPEVENT.Armed);
            }

            if (delta == MyAIO.DEVICESTATEDELTA.SAMPLING)
            {
                appstate.Event(APPEVENT.ContecTriggered);
            }

            //Handle bitflags

            //A device got Overflow error
            if (MyAIO.TestBit(allstatus, CaioConst.AIS_OFERR))
            {
                appstate.Event(APPEVENT.SamplingError);
                return false;
                //throw new ContecDeviceBufferOverflow();
            }

            //A device got Clock error
            if (MyAIO.TestBit(allstatus, CaioConst.AIS_SCERR))
            {
                appstate.Event(APPEVENT.SamplingError);
                return false;
                //throw new ContecClockSignalError();
            }

            //Collecting data || just got an Idle status after sampling
            if (
                MyAIO.TestBit(allstatus, CaioConst.AIS_DATA_NUM)
                || 
                delta == MyAIO.DEVICESTATEDELTA.READY )         //empty buffers
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

                {

                }
            }
            return false;

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
            //Make a copy into storage 
            myaio._concatdata = new DATA(myaio.GetConcatData());

            scope = new FmScope(myaio._concatdata, PersistentLoggerState.ps.data.n_channels, PersistentLoggerState.ps.data.duration);

            //Get new temp and add update AIOSettings.singleInstance
            string filepath = IO.GetFilePathTemp(PersistentLoggerState.ps.data);

            filepath = IO.CheckPath(filepath, false);

            IO.SaveDATA(PersistentLoggerState.ps.data, ref filepath, myaio._concatdata);

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
                fn = IO.CheckPath(fn, false);
            }

            RenameTempFile(fn);

        }

    }
}
