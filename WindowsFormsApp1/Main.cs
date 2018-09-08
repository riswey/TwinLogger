using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using System.Diagnostics;

using DEVICEID = System.Int16;
//The data structure is a dictionary; K: device id V:raw list of data for device
//K: device id :. data imported by id
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;
using System.Runtime.CompilerServices;

namespace MultiDeviceAIO
{
    public partial class Main : Form
    {

        MyAIO myaio;

        Monitor monitor;


        public DATA ConcatData
        {
            get
            {
                //This is backed by a variable
                return myaio.GetConcatData;
            }
        }

        //Handle Callback
        //static public GCHandle gCh;
        //static public MyAIO.PAICALLBACK pdelegate_func;
        //static public IntPtr pfunc;

        Timer timergetdata = new Timer();


        //1000hz, 64chan, 5sec
        //
        //int target = 10000;        //sampling freq x duration x 2
            //TODO: make per device


            //if returns 0 then its over for that device (end)

            //If target not met then can see who didn't get enough. Flag error
            //reset



        public Main()
        {
            if (!NativeMethods.CheckLibrary("caio.dll"))
            {
                NativeMethods.FailApplication("Driver error", "caio.dll\nNot found. Please install drivers.");
            }

            InitializeComponent();

            SetAIO();

            //Bindings
            loadBindData();

            //Set up accelerometers
            try
            {
                List<List<int>> mapping;
                IO.ReadCSV<int>(Monitor.fnMAPPING, IO.DelegateParseInt<int>, out mapping, ',', true);
                Accelerometer.ImportMapping(mapping, PersistentLoggerState.ps.data.n_channels);
            }
            catch (FileNotFoundException e)
            {
                throw new Exception("Cannot find mapping.csv");
            }

            Accelerometer.ImportCalibration(PersistentLoggerState.ps.data.caldata);


            //We can work this out exactly from amount of data coming in!
            timergetdata.Interval = MyAIO.TIMERPERIOD;

            timergetdata.Tick += data_Tick;

            //TODO: this must be stopped while sampling and restarted at end!!!
            TimerMonitorState(true);
        }

        //#CHECK
        //Advice from code checker
        ~Main()
        {
            if (myaio != null)
            {
                myaio.Close();
            }
        }

        void SetAIO()
        {
            //so can dynamically change AIO device binding (testing mode)
            if (myaio != null)
            {
                myaio = null;
                //need to implement Dispose!
                //myaio.Dispose();
            }
            myaio = new MyAIO(PersistentLoggerState.ps.data.testingmode);

            //Load Devices
            int devices_count = myaio.DiscoverDevices();

            PersistentLoggerState.ps.data.n_devices = devices_count;

            SetStatus(PersistentLoggerState.ps.data.n_devices + " Devices Connected");
        }

        /// <summary>
        /// Central handler for Device Exceptions
        /// </summary>
        /// <param name="ex"></param>
        /// <returns>
        /// false   - return/abort current operation
        /// </returns>
        bool ProcessError(AIODeviceException ex)
        {
            if (ex.code == 7)
            {
                myaio.ResetDevices();
                PrintLn("Device recovered from standby mode. Please try again.");
                SetStatus("Device recovered from standby mode. Please try again.");
                return false;
            }
            else
            {
                string msg = "Device Error: " + ex.code + ": " + ex.Message;
                PrintLn(msg);
                SaveLogFile();

                MessageBox.Show(msg, "Device Error\nDevices have been reset. Retry operation or manually reset devices again.", MessageBoxButtons.OK);

                //These are not critical errors! Can reset without affecting application state.
                //Application.Exit();
                myaio.ResetDevices();

                return false;
            }
        }

        void loadBindData()
        {
            cbMass.DataBindings.Clear();
            cbMass.DataBindings.Add("SelectedIndex", PersistentLoggerState.ps.data, "mass");

            chkClips.DataBindings.Clear();
            chkClips.DataBindings.Add("Checked", PersistentLoggerState.ps.data, "clipsOn");

            tbLoad.DataBindings.Clear();
            tbLoad.DataBindings.Add("Text", PersistentLoggerState.ps.data, "load");

            nudChannel.DataBindings.Clear();
            nudChannel.DataBindings.Add("Value", PersistentLoggerState.ps.data, "n_channels");

            nudDuration.DataBindings.Clear();
            nudDuration.DataBindings.Add("Value", PersistentLoggerState.ps.data, "duration");

            cbShaker.DataBindings.Clear();
            cbShaker.DataBindings.Add("SelectedIndex", PersistentLoggerState.ps.data, "shakertype");

            cbPad.DataBindings.Clear();
            cbPad.DataBindings.Add("SelectedIndex", PersistentLoggerState.ps.data, "paddtype");

            nudFreq.DataBindings.Clear();
            nudFreq.DataBindings.Add("Value", PersistentLoggerState.ps.data, "frequency");

            nudInterval.DataBindings.Clear();
            nudInterval.DataBindings.Add("Value", PersistentLoggerState.ps.data, "sample_frequency");

            tbDirectory.DataBindings.Clear();
            tbDirectory.DataBindings.Add("Text", PersistentLoggerState.ps.data, "testpath");

            chkExternalControl.DataBindings.Clear();
            chkExternalControl.DataBindings.Add("Checked", PersistentLoggerState.ps.data, "external_control");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //Give chance to close
            if (myaio != null)
            {
                myaio.Close();
            }

            //Commit final AIOSettings.singleInstance to app state 

            PersistentLoggerState.ps.Save("settings.xml");

            /*
            if (PersistentLoggerState.ps != null && PersistentLoggerState.ps.ExportXML(out string current_xml))
            {
                Properties.Settings.Default.processing_settings_current = current_xml;
                Properties.Settings.Default.Save();
            }
            */
            base.OnFormClosing(e);
        }

        private void data_Tick(object sender, EventArgs e)
        {
            //TODO: improve this state logic
            //check start sampling has reset everything
            //don't pass data around by value
            //

            //Can only do 1 thing at a time.

            int status = 0;

            PrintLn(myaio.running + ",", 0);

            if (!myaio.running)
                status = myaio.GetStatusAll();

            PrintLn(status + ",", 0);

            if ((status & (int)CaioConst.AIS_OFERR) == (int)CaioConst.AIS_OFERR)
            {
                PrintLn("Device Overflow");
                StartSampling();
                return;
            }

            if ((status & (int)CaioConst.AIS_SCERR) == (int)CaioConst.AIS_SCERR)
            {
                PrintLn("Sampling Clock Error");
                StartSampling();
                return;
            }

            //Loop until trigger pressed
            if ((status & (int)CaioConst.AIS_START_TRG) != (int)CaioConst.AIS_START_TRG)
            {
                myaio.running = true;
                setStartButtonText(true);
                RetrieveData();
                //Invoke(new Action(() => RetrieveData()));
            }

        }

        private void RetrieveData()
        {
            double percent = myaio.RetrieveAllData() / myaio.testtarget * 100;

            PrintLn(String.Format("{0:0.00}%",  percent)  , -1);

            //Take a few more samples to get the 0s that end the test 
            if (percent == 100.0)
            {
                PrintLn("Finishing...", 0);
            }

            if (myaio.IsTestFinished )
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

        void RenameTempFile(string fn)
        {
            if (fn == null)
            {
                PrintLn("Saving aborted.");
                SetStatus("Ready");
            }
            else
            {
                try
                {
                    if (IO.MoveTempFileAddHeader(PersistentLoggerState.ps, fn)) { PrintLn("Saved: \r\n" + fn); }
                    else { SetStatus("No data to save"); }
                }
                catch (IOException ex)
                {
                    PrintLn("Not Saved: " + ex.Message);
                    SetStatus("Error:" + ex.Message);
                }
            }
        }
        /*
        string GenerateDataReport(List<int> data)
        {
            String report = "";
            if (data.Count > 7)
            {
                report += "| A01: " + MyAIO.I2V(data[0]) + "," + MyAIO.I2V(data[1]) + "," + MyAIO.I2V(data[2]) + "\r\n";
                report += "| A02: " + MyAIO.I2V(data[3]) + "," + MyAIO.I2V(data[4]) + "," + MyAIO.I2V(data[5]) + "\r\n";
                report += "| A03: " + MyAIO.I2V(data[6]) + "," + MyAIO.I2V(data[7]) + "," + MyAIO.I2V(data[8]);
            }
            if (data.Count > 28)
            {
                report += "\r\n| A10: " + MyAIO.I2V(data[27]) + "," + MyAIO.I2V(data[28]) + "," + MyAIO.I2V(data[29]);
            }

            int end = data.Count;

            report += "\r\n| A" + end + ": " + MyAIO.I2V(data[end - 3]) + "," + MyAIO.I2V(data[end - 2]) + "," + MyAIO.I2V(data[end - 1]);

            return report;
        }
        */
        void SetStatus(string msg)
        {
            toolStripStatusLabel1.Text = msg;
        }

        void PrintLn(object msg, int linebreak = 1)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => PrintLn(msg, linebreak)));
                return;
            }

            //
            switch(linebreak)
            {
                case -1:
                    string[] txt = textBox1.Text.Split('\n');
                    textBox1.Text = String.Join("\n", txt, 0, txt.Length - 2) + "\n" + msg.ToString() + "\r\n";
                    break;
                case 0:
                    textBox1.Text += msg.ToString();
                    break;
                case 1:
                    textBox1.Text += msg.ToString() + "\r\n";
                    break;
            }

            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        void SaveLogFile()
        {
            File.WriteAllText(PersistentLoggerState.ps.data.testpath + @"\log.txt", textBox1.Text);
        }

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

            pfunc = Marshal.GetFunctionPointerForDelegate(pdelegate_func);
            myaio.SetAiCallBackProc(pfunc);


            //STOP TIMER
            TimerMonitorState(false);

            PrintLn("Start");

            myaio.Start();
            timergetdata.Start();
        }

        private void SelectDirectory()
        {
            string startLocation = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            //string startLocation = Environment.SpecialFolder.Desktop.ToString();

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = startLocation;

                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !fbd.SelectedPath.IsNullOrWhiteSpace())
                {
                    PersistentLoggerState.ps.data.testpath = fbd.SelectedPath;
                    loadBindData();
                }
            }
        }


        /////////////////////////////////////////////////////////////////////////
        // GUI EVENTS
        /////////////////////////////////////////////////////////////////////////
        /*
        private void SetFilename() {
            string filepath;
            //get default filename
            if (checkBox1.Checked)
            {
                txtFilepath.Text = IO.GetFilePathCal(PersistentLoggerState.ps.data, cbOrientation.SelectedIndex);
            }
            else
            {
                txtFilepath.Text = IO.GetFilePathTest(PersistentLoggerState.ps.data);
            }
        }
        */
        private void monitorChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            monitor = new Monitor(PersistentLoggerState.ps.data.n_channels);
            monitor.Show();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartSampling();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (tbDirectory.Text == "")
            {
                if (MessageBox.Show("Warning", "No filename set", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    return;
                }
            }

            setStartButtonText(true, chkExternalControl.Checked);

            PrintLn("----------------------------------------------------\r\nApplied Settings");
            PrintLn(PersistentLoggerState.ps.ToString());

            StartSampling();
        }

        void setStartButtonText(bool on, bool external = false)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => setStartButtonText(on, external)));
                return;
            }

            Button b = this.btnStart;

            int code = ((on ? 1 : 0) + (external ? 2 : 0) );

            switch (code)
            {
                case 0:
                case 2:
                    b.Text = "Start";
                    b.BackColor = Color.Transparent;
                    b.Font = new Font("Microsoft Sans Serif", 15.75F, FontStyle.Regular);
                    break;
                case 1:
                    b.Text = "Sampling...";
                    b.BackColor = Color.Orange;
                    b.Font = new Font("Microsoft Sans Serif", 10.25F, FontStyle.Bold);
                    break;
                case 3:
                    b.Text = "Armed";
                    b.BackColor = Color.Orange;
                    b.Font = new Font("Microsoft Sans Serif", 15.75F, FontStyle.Bold);
                    break;
                default:
                    break;
            }
            b.Refresh();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("What is this?");
            //string fn = UserInputAfterSampling();
            //RenameTempFile(fn);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SelectDirectory();
        }

        private void selectDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectDirectory();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                label11.ForeColor = Color.Gray;
                label12.ForeColor = Color.Gray;
                label13.ForeColor = Color.Black;
                checkBox1.Text = "ON";
                checkBox1.BackColor = Color.Orange;
            }
            else
            {
                label11.ForeColor = Color.Black;
                label12.ForeColor = Color.Black;
                label13.ForeColor = Color.Gray;
                checkBox1.Text = "OFF";
                checkBox1.BackColor = Color.Transparent;
            }
        }

        //Toolstrip Settings events

        private void loadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            loadSettings();
        }

        private void saveAsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (PersistentLoggerState.ps.data.settingspath != "")
                PersistentLoggerState.ps.Save(PersistentLoggerState.ps.data.settingspath);
            else
                saveSettings();
        }

        private void resetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            PersistentLoggerState.ps.Reload();
            loadBindData();
            displayPath(PersistentLoggerState.ps.data.settingspath, PersistentLoggerState.ps.data.modified);
        }

        private void resetDevicesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                myaio.ResetDevices();
            }
            catch (AIODeviceException ex)
            {
                ProcessError(ex);
            }

            PersistentLoggerState.ps.data.n_devices = myaio.DiscoverDevices();

            SetStatus(PersistentLoggerState.ps.data.n_devices + " Devices Connected");
        }

        ////////////////////////////////////////////////////////////////////////////////////
        // SETTINGS
        ////////////////////////////////////////////////////////////////////////////////////

        //Flag to stop initial databinding setting everything dirty
        bool has_loaded = false;

        void loadSettings()
        {
            //Load
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "config files|*.xml";
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    has_loaded = false;

                    string filename = openFileDialog1.FileName;

                    PersistentLoggerState.ps.Load(filename);

                    loadBindData();

                    displayPath(filename, PersistentLoggerState.ps.data.modified);

                    //So can reset
                    //auto saved
                    //PersistentLoggerState.ps.data.settingspath = filename;

                    has_loaded = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        void saveSettings()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "conf files |*.xml";
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filename = saveFileDialog1.FileName;
                    PersistentLoggerState.ps.Save(filename);
                    //auto sets settingspath
                    displayPath(filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. " + ex.Message);
                }
            }
        }

        void resetSettings()
        {
            //Reset to base
            has_loaded = false;
            PersistentLoggerState.ps.Reload();
            loadBindData();
            displayPath(PersistentLoggerState.ps.data.settingspath);
            has_loaded = true;
        }

        void displayPath(string path, bool modified = false)
        {

            string fn = IO.getFileName(path);
            string fn1 = fn + ((modified) ? " (modified)" : "");

            toolStripStatusLabel1.Text = "Current config: " + fn1;

            //System.Collections.Specialized.StringCollection sc = Properties.Settings.Default.processing_AIOSettings.singleInstance_prev;

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().Show();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "data files|*.csv";
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                has_loaded = false;

                string filename = openFileDialog1.FileName;

                (new Scope(filename)).Show();
            }
        }

        private void scopeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: inefficient as must convert data every time
            //Can scope take the dictionary?

            using (Scope scope = new Scope(myaio.GetConcatData, PersistentLoggerState.ps.data.n_channels, PersistentLoggerState.ps.data.duration))
            {
                if (!scope.IsDisposed)
                {
                    scope.Show();
                }
            }

        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool originalTesting = PersistentLoggerState.ps.data.testingmode; 
            using (var form = new UserSettings(PersistentLoggerState.ps.data))
            {
                var res = form.ShowDialog();
                if (res == DialogResult.OK)
                {
                    if (originalTesting != PersistentLoggerState.ps.data.testingmode)
                    {
                        //change of state
                        SetAIO();
                    }
                }
            }
        }

        private void calibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Monitor.LoadAccelometerCalibration();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Do status stuff
            var snapshot = myaio.ChannelsSnapShotBinary(PersistentLoggerState.ps.data.n_channels);
            Accelerometer.setChannelData(snapshot);

            DrawStatus(Accelerometer.ArrayStatus());

            if (monitor != null) monitor.ReDraw();
        }

        private void DrawStatus(int status)
        {
            switch(status)
            {
                case 0:
                    pbStatusOut.Image = MultiDeviceAIO.Properties.Resources.red;
                    pbStatus.Image = MultiDeviceAIO.Properties.Resources.grey;
                    break;
                case 1:
                    pbStatusOut.Image = MultiDeviceAIO.Properties.Resources.grey;
                    pbStatus.Image = MultiDeviceAIO.Properties.Resources.green;
                    break;
                default:
                    pbStatusOut.Image = MultiDeviceAIO.Properties.Resources.grey;
                    pbStatus.Image = MultiDeviceAIO.Properties.Resources.grey;
                    break;
            }
        }

        private void TimerMonitorState(bool on)
        {
            if (on)
            {
                timermonitor.Start();
            } else
            {
                pbStatus.Image = MultiDeviceAIO.Properties.Resources.grey;
                timermonitor.Stop();
            }
        }

        private void setupToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }
        
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                //Abort
                timergetdata.Stop();
                myaio.Stop();
                myaio.ResetTest();
                myaio.ResetDevices();
                setStartButtonText(false);
                PrintLn("Run aborted");
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        /**************************************************
         * NEED A CALLBACK TO KNOW WHEN TRIGGER STARTED
         * IT WILL NOT ALLOW STATE REQUEST WHILE RUNNING
         *************************************************/

        static public GCHandle gCh;
        static public MyAIO.PAICALLBACK pdelegate_func;
        static public IntPtr pfunc;

        unsafe private void Form_AiCall_Load(object sender, System.EventArgs e)
        {
            // Initialize the delegate for event notification
            pdelegate_func = new MyAIO.PAICALLBACK(CallBackProc);
            // Do not release the delegate
            if (gCh.IsAllocated == false)
            {
                gCh = GCHandle.Alloc(pdelegate_func);
            }
        }

        private void Form_AiCall_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Release the garbage collection handle of the delegate for event notification
            if (gCh.IsAllocated == true)
            {
                gCh.Free();
            }
        }

        //CallBacks
        unsafe public int CallBackProc(short Id, short Message, int wParam, int lParam, void* Param)
    {
        if (InvokeRequired)
        {
            //Put on main thread anyway
            //Invoke(new Action(() => CallBackProc(Id, Message, wParam, lParam, Param)));
            //return 0;
        }

        //wParam???;
        int num_samples = lParam;

        switch ((CaioConst)Message)
        {
            case CaioConst.AIOM_AIE_DATA_NUM:
                myaio.RetrieveData(Id, num_samples, PersistentLoggerState.ps.data.n_channels);
                PrintLn(Id, false);
                break;
            case CaioConst.AIOM_AIE_END:
                myaio.RetrieveData(Id, num_samples, PersistentLoggerState.ps.data.n_channels);
                myaio.DeviceFinished(Id);
                if (myaio.IsTestFinished())
                {
                    TestFinished(Id, num_samples);
                }
                break;
            case CaioConst.AIOM_AIE_OFERR:
                {
                    string status = myaio.GetStatus(Id);
                    myaio.Stop();
                    myaio.ResetTest();
                    setStartButtonText(false);
                    PrintLn(String.Format("[Overflow error on device {0}. Status: {1} (Test reset)]", Id, status), false);
                    //overflow error
                }
                break;
            case CaioConst.AIOM_AIE_SCERR:
                {
                    string status = myaio.GetStatus(Id);
                    myaio.Stop();
                    myaio.ResetTest();
                    setStartButtonText(false);
                    PrintLn(String.Format("[Sampling clock error on device {0}. Status: {1} (Test reset)]", Id, status), false);
                }
                break;
            case CaioConst.AIOM_AIE_ADERR:
                PrintLn("\r\nData conversion error.");
                break;
        }
        return 0;
    }





}
}
