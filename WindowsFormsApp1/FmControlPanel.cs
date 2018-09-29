using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using MotorController;

using DEVICEID = System.Int16;
//The data structure is a dictionary; K: device id V:raw list of data for device
//K: device id :. data imported by id
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MultiDeviceAIO
{
    public partial class FmControlPanel : Form
    {

        #region INIT

        bool probationLoadedstate = false;

        MyAIO myaio;
        FmMonitor monitor;
        FmLog fmlog = new FmLog();
        FmScope scope;
        /*
        public DATA ConcatData
        {
            get
            {
                //This is backed by a variable
                return myaio.GetConcatData();
            }
        }
        */
        //1000hz, 64chan, 5sec
        //
        //int target = 10000;        //sampling freq x duration x 2
        //TODO: make per device


        //if returns 0 then its over for that device (end)

        //If target not met then can see who didn't get enough. Flag error
        //reset

        public FmControlPanel()
        {
            if (!NativeMethods.CheckLibrary("caio.dll"))
            {
                NativeMethods.FailApplication("Driver error", "caio.dll\nNot found. Please install drivers.");
            }

            //contecpoller.Interval = CONTECPOLLERSTATE;

            InitializeComponent();

            SetupAppStateMachine();

            setStartButtonText(0);

            pbr0.Maximum = pbr1.Maximum = 100;

            //Bindings
            BindTestParameters();
            BindMotorControls();

            serialpoller.Interval = PersistentLoggerState.ps.data.arduinotick;
            InitMotorStateMachine();
            InitFmCPMotorControl();

            //Set up accelerometers
            try
            {
                List<List<int>> mapping;
                IO.ReadCSV<int>(FmMonitor.fnMAPPING, IO.DelegateParseInt<int>, out mapping, ',', true);
                Accelerometer.ImportMapping(mapping, PersistentLoggerState.ps.data.n_channels);
            }
            catch (FileNotFoundException e)
            {
                throw new Exception("Cannot find mapping.csv");
            }

            Accelerometer.ImportCalibration(PersistentLoggerState.ps.data.caldata);

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
            DiscoverDevices();

        }

        void BindTestParameters()
        {
            //TODO: Need to clear before rebinding! Quicker way

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

            nudFreqFrom.DataBindings.Clear();
            nudFreqFrom.DataBindings.Add("Value", PersistentLoggerState.ps.data, "freq_from");

            nudFreqTo.DataBindings.Clear();
            nudFreqTo.DataBindings.Add("Value", PersistentLoggerState.ps.data, "freq_to");

            nudFreqStep.DataBindings.Clear();
            nudFreqStep.DataBindings.Add("Value", PersistentLoggerState.ps.data, "freq_step");

            nudInterval.DataBindings.Clear();
            nudInterval.DataBindings.Add("Value", PersistentLoggerState.ps.data, "sample_frequency");

            tbDirectory.DataBindings.Clear();
            tbDirectory.DataBindings.Add("Text", PersistentLoggerState.ps.data, "testpath");

            chkExternalTrigger.DataBindings.Clear();
            chkExternalTrigger.DataBindings.Add("Checked", PersistentLoggerState.ps.data, "external_trigger");

            chkExternalClock.DataBindings.Clear();
            chkExternalClock.DataBindings.Add("Checked", PersistentLoggerState.ps.data, "external_clock");

        }

        void DiscoverDevices()
        {

            //NEW CODE: hanging on this alot
            Cursor = Cursors.WaitCursor; // change cursor to hourglass type
            var task = Task.Run(() =>
            {
                int devices_count = myaio.DiscoverDevices();
                PersistentLoggerState.ps.data.n_devices = devices_count;
            });
            if (task.Wait(TimeSpan.FromSeconds(30)))
            {
                Cursor = Cursors.Arrow; // change cursor to normal type
                SetStatus(PersistentLoggerState.ps.data.n_devices + " Devices Connected");
                if (PersistentLoggerState.ps.data.n_devices < 2)
                {
                    if (MessageBox.Show(this, "Not all devices are connected. Try Auto-Reset?", "Device Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        myaio.ResetDevices();
                        DiscoverDevices();
                        return;
                    }
                }
                return;
            }
            else
            {
                if (MessageBox.Show(this, "Unable to initialise Contec devices.", "Device Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                {
                    //null so that it doesn't interact with devices anymore in OnClosing
                    myaio = null;
                    Close();
                }
            }

        }

        private void FmControlPanel_Shown(object sender, EventArgs e)
        {
            //Now sort Contec devices
            SetAIO();

            //Draw statusstrip
            DrawContecStatusStrip();

            //TODO: We can work this out exactly from amount of data coming in!
            //timergetdata.Interval = ;

            //TODO: this should be stopped while sampling
            monitorpoller.Start();

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            MotorCleanUp();

            appstate.Save();
            sm_motor.Save();

            //Give chance to close  
            myaio?.Dispose();

            //Commit final AIOSettings.singleInstance to app state 
            PersistentLoggerState.ps.Save("settings.xml");

            base.OnFormClosing(e);
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
                    if (IO.MoveTempFileAddHeader(PersistentLoggerState.ps, fn)) { PrintLn("> " + fn); }
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

        void PrintLn(object msg, bool speak = false, int linebreak = 1)
        {
            if (fmlog == null || fmlog.IsDisposed)
            {
                fmlog = new FmLog();
            }

            fmlog.Show();
            fmlog.WindowState = FormWindowState.Minimized;
            fmlog.PrintLn(msg, speak, linebreak);

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
                    BindTestParameters();
                }
            }
        }
        #endregion

        #region GUI Events

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
        private void closeToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void monitorChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            monitor = new FmMonitor(PersistentLoggerState.ps.data.n_channels);
            monitor.Show();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //InitRun();
            appstate.Event(APPEVENT.InitRun);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //InitRun();
            appstate.Event(APPEVENT.InitRun);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            appstate.Event(APPEVENT.Stop);
            //StopScheduleRun();
        }

        string startbuttontext = "Ready";

        void setStartButtonText(int code)
        {
            //TODO: You can take a APPSTATE parameter

            if (InvokeRequired)
            {
                this.Invoke(new Action(() => setStartButtonText(code)));
                return;
            }

            Button b = this.btnStart;

            switch (code)
            {
                case 0:
                    b.Text = "Start Sequence";
                    b.BackColor = Color.Transparent;
                    b.Font = new Font("Microsoft Sans Serif", 15.75F, FontStyle.Regular);
                    break;
                case 1:
                    b.Text = "Armed";
                    b.BackColor = Color.Orange;
                    b.Font = new Font("Microsoft Sans Serif", 15.75F, FontStyle.Bold);
                    break;
                case 2:
                    b.Text = "Sampling...";
                    b.BackColor = Color.Orange;
                    b.Font = new Font("Microsoft Sans Serif", 10.25F, FontStyle.Bold);
                    break;
                case 3:
                    b.Text = "Running";
                    b.BackColor = Color.Orange;
                    b.Font = new Font("Microsoft Sans Serif", 10.25F, FontStyle.Bold);
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
            //TODO: Is this obsolete?
            PersistentLoggerState.ps.Reload();
            BindTestParameters();
            displayPath(PersistentLoggerState.ps.data.settingspath, PersistentLoggerState.ps.data.modified);
        }

        private void resetDevicesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            monitorpoller.Stop();
            myaio.RefreshDevices();
            myaio.ResetDevices();
            DiscoverDevices();
            monitorpoller.Start();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().Show();
        }

        private void scopeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: very inefficient as must convert data every time, and create a new scope
            //Can scope take the dictionary?
            
            scope = new FmScope(myaio._concatdata, PersistentLoggerState.ps.data.n_channels, PersistentLoggerState.ps.data.duration);

            if (!scope?.IsDisposed ?? false)
            {
                scope.Show();
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int originalTesting = PersistentLoggerState.ps.data.testingmode;
            using (var form = new FmOptions(PersistentLoggerState.ps.data))
            {
                var res = form.ShowDialog();
                if (res == DialogResult.OK)
                {
                    if (originalTesting != PersistentLoggerState.ps.data.testingmode)
                    {
                        //change of testing state
                        SetAIO();
                    }
                }
            }
        }

        private void calibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FmMonitor.LoadAccelometerCalibration();
        }

        /*private void motorControllerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new MotorController.MotorController()).Show();
        }*/

        private void button1_Click(object sender, EventArgs e)
        {
            appstate.Event(APPEVENT.Stop);
            //Abort();
        }

        float graphrange = 1;
        public void btnIncRange_Click(object sender, EventArgs e)
        {
            graphrange *= 2.0f;
            UpdateChartYScale();
        }

        public void btnDecRange_Click(object sender, EventArgs e)
        {
            graphrange /= 2.0f;
            UpdateChartYScale();
        }

        private void stopESCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            appstate.Event(APPEVENT.Stop);
            //Abort();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                //Abort
                appstate.Event(APPEVENT.Stop);
                //Abort();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            //TODO: overrides app state. Beware!
            sm_motor.Event(ARDUINOEVENT.Send_Trigger);
        }

        #endregion

        #region SETTINGS

        //Flag to stop initial databinding setting everything dirty
        bool has_loaded = false;

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "data files|*.csv";
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                has_loaded = false;

                string filename = openFileDialog1.FileName;

                (new FmScope(filename)).Show();
            }
        }

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

                    //TODO: better way to refresh parameters?
                    BindTestParameters();

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
            BindTestParameters();
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

        #endregion

        #region LEDs

        private void monitorPoller_Tick(object sender, EventArgs e)
        {
            if (!appstate.IsState(APPSTATE.Ready))
            {
                pbStatus.Image = MultiDeviceAIO.Properties.Resources.grey;
                monitorChannelsToolStripMenuItem.Enabled = false;
                return;
            }

            int rtncode;
            if ((rtncode = myaio.ContecOK()) != 0)
            {
                monitorpoller.Stop();
                switch (MessageBox.Show(this, "Contec device error. Returned code " + rtncode + "\nReentry?, Ignore or Close", "Contec Error", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error))
                {
                    case DialogResult.Yes:
                        myaio.ResetDevices();
                        DiscoverDevices();
                        monitorpoller.Start();
                        return;
                    case DialogResult.Cancel:
                        this.Close();
                        return;
                }
            }

            monitorChannelsToolStripMenuItem.Enabled = true;

            //Do accelermonter status stuff
            var snapshot = myaio.ChannelsSnapShotBinary(PersistentLoggerState.ps.data.n_channels);
            Accelerometer.setChannelData(snapshot);

            DrawAccArrayStatus(Accelerometer.ArrayStatus());

            if (monitor != null) monitor.ReDraw();
        }

        private void DrawAccArrayStatus(int status)
        {
            switch (status)
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

        //TODO: these could be passed as an array to the device and have it DrawStatus()
        private void DrawContecStatusStrip()
        {
            DrawContecStatusStrip(new int[0]);
        }

        private void DrawContecStatusStrip(int[] status)
        {
            if (status.Length == 0) return;

            int s;
            s = status[0];
            DrawStatus(pb1ok, s == 0 ? 1 : 0);
            DrawStatus(pb1busy, s & (int)CaioConst.AIS_BUSY);
            DrawStatus(pb1arm, s & (int)CaioConst.AIS_START_TRG);
            DrawStatus(pb1data, s & (int)CaioConst.AIS_DATA_NUM);
            DrawStatus(pb1overflow, s & (int)CaioConst.AIS_OFERR);
            DrawStatus(pb1timer, s & (int)CaioConst.AIS_SCERR);
            DrawStatus(pb1convert, s & (int)CaioConst.AIS_AIERR);
            DrawStatus(pb1device, s & (int)CaioConst.AIS_DRVERR);

            if (status.Length > 1)
            {
                s = status[1];
                DrawStatus(pb2ok, s == 0 ? 1 : 0);
                DrawStatus(pb2busy, s & (int)CaioConst.AIS_BUSY);
                DrawStatus(pb2arm, s & (int)CaioConst.AIS_START_TRG);
                DrawStatus(pb2data, s & (int)CaioConst.AIS_DATA_NUM);
                DrawStatus(pb2overflow, s & (int)CaioConst.AIS_OFERR);
                DrawStatus(pb2timer, s & (int)CaioConst.AIS_SCERR);
                DrawStatus(pb2convert, s & (int)CaioConst.AIS_AIERR);
                DrawStatus(pb2device, s & (int)CaioConst.AIS_DRVERR);
            }
        }

        private void DrawStatus(PictureBox pb, int state)
        {
            if (state == 0)
            {
                pb.Image = Properties.Resources.grey;
            }
            else
            {
                if (state < 65536)
                    pb.Image = Properties.Resources.green;
                else
                    pb.Image = Properties.Resources.red;
            }
        }

        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            doSend(CMD.SETFREQ, "30");
            SendCommand(CMD.START);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SendCommand(CMD.STOP);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> dict = LoggerState.MergeDictionary<MotorPropertyAttribute>(PersistentLoggerState.ps.data);
            string text = String.Join("}, {", dict.Keys.ToArray());
            MessageBox.Show("{" + text + "}");
        }
    }
}
