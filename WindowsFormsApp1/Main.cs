using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using System.Diagnostics;
using System.Threading;

using DEVICEID = System.Int16;
//The data structure is a dictionary; K: device id V:raw list of data for device
//K: device id :. data imported by id
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;


namespace MultiDeviceAIO
{
    public partial class Main : Form
    {
        static string DEVICE_ROOT = "Aio00";

        MyAIO myaio;
        PersistentLoggerState ps;

        public Main(PersistentLoggerState ps)
        {
            if (!NativeMethods.CheckLibrary("caio.dll"))
            {
                NativeMethods.FailApplication("Driver error", "caio.dll\nNot found. Please install drivers.");
            }

            InitializeComponent();

            //AIO
            myaio = new MyAIO();
            int devices_count = myaio.DiscoverDevices(DEVICE_ROOT);

            //Settings
            this.ps = ps;

            ps.data.n_devices = devices_count;

            SetStatus(ps.data.n_devices + " Devices Connected");
            
            //Bindings
            loadBindData();
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
            cbMass.DataBindings.Add("SelectedIndex", ps.data, "mass");

            chkClips.DataBindings.Clear();
            chkClips.DataBindings.Add("Checked", ps.data, "clipsOn");

            tbLoad.DataBindings.Clear();
            tbLoad.DataBindings.Add("Text", ps.data, "load");

            nudChannel.DataBindings.Clear();
            nudChannel.DataBindings.Add("Value", ps.data, "n_channels");

            nudDuration.DataBindings.Clear();
            nudDuration.DataBindings.Add("Value", ps.data, "duration");

            cbShaker.DataBindings.Clear();
            cbShaker.DataBindings.Add("SelectedIndex", ps.data, "shakertype");

            cbPad.DataBindings.Clear();
            cbPad.DataBindings.Add("SelectedIndex", ps.data, "paddtype");

            nudInterval.DataBindings.Clear();
            nudInterval.DataBindings.Add("Value", ps.data, "sample_frequency");

            tbDirectory.DataBindings.Clear();
            tbDirectory.DataBindings.Add("Text", ps.data, "testpath");

            chkExternalTrigger.DataBindings.Clear();
            chkExternalTrigger.DataBindings.Add("Checked", ps.data, "external_trigger");

            chkExternalClock.DataBindings.Clear();
            chkExternalClock.DataBindings.Add("Checked", ps.data, "external_clock");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //Give chance to close
            if (myaio != null)
            {
                myaio.Close();
            }

            //Commit final AIOSettings.singleInstance to app state 
            string current_xml;
            if (ps != null && ps.ExportXML(out current_xml))
            {
                //failed to get XML
                Properties.Settings.Default.processing_settings_current = current_xml;
                Properties.Settings.Default.Save();
            }
            base.OnFormClosing(e);
        }

        //////////////////////////////////////////////////////////////////////
        // MESSAGE LOOP
        //////////////////////////////////////////////////////////////////////

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {
                        short device_id = (short)m.WParam;
                        int num_samples = (int)m.LParam;

                        try
                        {
                            myaio.DeviceFinished(device_id, num_samples, ps.data.n_channels);
                        } catch (AIODeviceException ex)
                        {
                            ProcessError(ex);
                            return;
                        }

                        if (myaio.IsTestFinished())
                        {
                            TestFinished(device_id, num_samples);
                        }
                    }
                    break;
                case 0x1003:
                    {
                        short device_id = (short)m.WParam;
                        int num_samples = (int)m.LParam;
                        try {
                            myaio.RetrieveData(device_id, num_samples, ps.data.n_channels);
                        }
                        catch (AIODeviceException ex)
                        {
                            ProcessError(ex);
                        }

                        PrintLn(device_id, false);
                    }
                    break;
            }

            base.WndProc(ref m);
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
        void TestFinished(short device_id, int num_samples)
        {
            //Per device list of results (int[])
            
            DATA concatdata;
            myaio.GetData(out concatdata);

            //Delete existing temp file
            if (ps.data.temp_filename != null)
            {
                File.Delete(ps.data.temp_filename);
            }
            //Get new temp and add update AIOSettings.singleInstance
            string filepath = IO.GetFilePathTemp(ps.data);

            IO.SaveDATA(ps.data, filepath, concatdata);

            PrintLn("END");

            //Generate User reports to see what happened

            PrintLn("+---Report-----------------------------------------");
            foreach(KeyValuePair<DEVICEID, List<int>> device_data in concatdata)
            {
                PrintLn("| Device: " + myaio.devicenames[device_data.Key] + " (" + device_data.Value.Count + ")");
                PrintLn(GenerateDataReport(device_data.Value));
            }
            PrintLn("+--------------------------------------------------");

            //SAVE LOG
            SaveLogFile();

            //Produce scope
            (new Scope(concatdata, ps.data.n_channels, ps.data.duration)).Show();

            SetStatus("Awaiting User Input...");

            //Now ask user input to save
            //Provide a freq -> complete header. Save header
            string fn = UserInputAfterSampling();

            string fp = tbDirectory.Text + @"\" + fn + ".csv";

            RenameTempFile(fp);

            SetStatus("Ready");
        }

        string UserInputAfterSampling()
        {
            //User decides:
            //freq
            //filename
            var filepath = txtFilepath.Text;

            using (UserCompleteTest testDialog = new UserCompleteTest(filepath))
            {
                if (testDialog.ShowDialog(this) == DialogResult.OK)
                {
                    ps.data.frequency = (float)testDialog.nudFreq.Value;
                    filepath = testDialog.tbFilename.Text;
                    PrintLn("Frequency: \t" + ps.data.frequency);
                }
                else
                {
                    //Abort
                    filepath = null;
                }
            }

            return filepath;
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
                    if (IO.MoveTempFile(ps, fn)) { PrintLn("Saved: " + fn); }
                    else { SetStatus("No data to save"); }
                }
                catch (IOException ex)
                {
                    PrintLn("Not Saved: " + ex.Message);
                    SetStatus("Error:" + ex.Message);
                }
            }
        }

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

        void SetStatus(string msg)
        {
            toolStripStatusLabel1.Text = msg;
        }

        void PrintLn(object msg, bool linebreak = true)
        {
            textBox1.Text += msg.ToString() + (linebreak ? "\r\n" : "");
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        void SaveLogFile()
        {
            File.WriteAllText(ps.data.testpath + @"\log.txt", textBox1.Text);
        }

        void StartSampling()
        {
            if (ps.data.n_devices == 0)
            {
                SetStatus("Warning: No Devices connected. Reset");
                return;
            }
            
            List<int> failedID;
            if (!myaio.DeviceCheck(ps.data.n_channels, out failedID))
            {
                string status = "Error: Devices not responding. Device(s) ";
                foreach (short f in failedID)
                {
                    status += myaio.devicenames[f] + " ";
                }

                status += "failed.";

                SetStatus(status);
                return;
            }

            ////////////////////////////////////////////////////////////////////
            // RESET DATA HERE
            ////////////////////////////////////////////////////////////////////
            try {
                myaio.ResetTest();
            }
            catch (AIODeviceException ex)
            {
                ProcessError(ex);
            }

            var num_samples = nudDuration.Value * (decimal)1E6 / nudInterval.Value;

            PrintLn("----------------------------------------------------\r\nApplied Settings");
            PrintLn(ps.ToString());

            try {
                myaio.SetupTimedSample(ps.data);
            }
            catch (AIODeviceException ex)
            {
                ProcessError(ex);
            }

            try {
                myaio.Start((uint)this.Handle.ToInt32());
            }
            catch (AIODeviceException ex)
            {
                ProcessError(ex);
            }

            SetStatus("Sampling...");
            PrintLn("Sampling...", false);
        }

        private void SelectDirectory()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !fbd.SelectedPath.IsNullOrWhiteSpace())
                {
                    ps.data.testpath = fbd.SelectedPath;
                    loadBindData();
                }
            }
        }





        /////////////////////////////////////////////////////////////////////////
        // GUI EVENTS
        /////////////////////////////////////////////////////////////////////////

        private void SetFilename() {
            string filepath;
            //get default filename
            if (checkBox1.Checked)
            {
                txtFilepath.Text = IO.GetFilePathCal(ps.data, cbOrientation.SelectedIndex);
            }
            else
            {
                txtFilepath.Text = IO.GetFilePathTest(ps.data);
            }
        }

        private void monitorChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Monitor(myaio, ps.data.n_channels)).Show();
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
            if (txtFilepath.Text == "")
            {
                if (MessageBox.Show("Warning", "No filename set", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    return;
                }
            }

            StartSampling();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fn = UserInputAfterSampling();
            RenameTempFile(fn);
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
                this.BackColor = Color.Orchid;
            }
            else
            {
                this.BackColor = SystemColors.Control;
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
            if (ps.data.path != "")
                ps.Save(ps.data.path);
            else
                saveSettings();
        }

        private void resetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ps.Reload();
            loadBindData();
            displayPath(ps.data.path, ps.data.modified);
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

            ps.data.n_devices = myaio.DiscoverDevices(DEVICE_ROOT);

            SetStatus(ps.data.n_devices + " Devices Connected");
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

                    ps.Load(filename);

                    loadBindData();

                    displayPath(filename, ps.data.modified);

                    //So can reset
                    ps.data.path = filename;

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
                    ps.Save(filename);
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
            ps.Reload();
            loadBindData();
            displayPath(ps.data.path);
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
            DATA concatdata;
            myaio.GetData(out concatdata);

            SetStatus("Loaded: " + concatdata.Count);

            (new Scope(concatdata, ps.data.n_channels, ps.data.duration)).Show();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new UserSettings(ps.data)).Show();
        }
    }
}
