using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

//Dllimport
using System.Runtime.InteropServices;
using System.Threading;

using System.Diagnostics;
using System.IO;

namespace MultiDeviceAIO
{
    public partial class Main : Form
    {
        static string DEVICE_ROOT = "Aio00";

        MyAIO myaio;
        AIOSettings settings = new AIOSettings();

        /*
         * Check the dll exists
        */

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hModule);

        static bool CheckLibrary(string fileName)
        {
            IntPtr hinstLib = LoadLibrary(fileName);
            FreeLibrary(hinstLib);
            return hinstLib != IntPtr.Zero;
        }


        public Main()
        {
            InitializeComponent();

            if (!CheckLibrary("caio.dll"))
            {
                new Thread(new ThreadStart(delegate
                {
                    MessageBox.Show
                    (
                      "Caio.dll not found\nPlease install drivers that came with the device",
                      "Driver error",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error
                    );
                })).Start();

                Application.Exit();
            }

            //Load settings from app state
            if (Properties.Settings.Default.processing_settings_current == "")
            {
                Properties.Settings.Default.processing_settings_current = SettingData.default_xml;
            }

            settings.ImportXML(Properties.Settings.Default.processing_settings_current);

            loadBindData();

            //Init AIO
            myaio = new MyAIO();
            settings.data.n_devices = myaio.DiscoverDevices(DEVICE_ROOT);


            SetStatus(settings.data.n_devices + " Devices Connected");

        }

        ~Main()
        {
            myaio.Close();
        }

        void loadBindData()
        {
            cbMass.DataBindings.Clear();
            cbMass.DataBindings.Add("SelectedIndex", settings.data, "mass");

            chkClips.DataBindings.Clear();
            chkClips.DataBindings.Add("Checked", settings.data, "clipsOn");

            tbLoad.DataBindings.Clear();
            tbLoad.DataBindings.Add("Text", settings.data, "load");

            nudChannel.DataBindings.Clear();
            nudChannel.DataBindings.Add("Value", settings.data, "n_channels");

            nudDuration.DataBindings.Clear();
            nudDuration.DataBindings.Add("Value", settings.data, "duration");

            cbShaker.DataBindings.Clear();
            cbShaker.DataBindings.Add("SelectedIndex", settings.data, "shakertype");

            cbPad.DataBindings.Clear();
            cbPad.DataBindings.Add("SelectedIndex", settings.data, "paddtype");

            nudInterval.DataBindings.Clear();
            nudInterval.DataBindings.Add("Value", settings.data, "timer_interval");

            tbDirectory.DataBindings.Clear();
            tbDirectory.DataBindings.Add("Text", settings.data, "testpath");

            chkExternalTrigger.DataBindings.Clear();
            chkExternalTrigger.DataBindings.Add("Checked", settings.data, "external_trigger");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //Give chance to close
            myaio.Close();

            //Commit final settings to app state 
            string current_xml = settings.ExportXML();
            Properties.Settings.Default.processing_settings_current = current_xml;
            Properties.Settings.Default.Save();
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

                        long ret = myaio.DeviceFinished(device_id, num_samples, settings.data.n_channels);
                        ProcessReturnValues(ret);

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
                        long ret = myaio.RetrieveData(device_id, num_samples, settings.data.n_channels);
                        ProcessReturnValues(ret);

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
            List<List<int>> concatdata;
            myaio.GetData(out concatdata);

            string filepath = IO.GetFilePathTemp(settings.data);

            IO.SaveArray(settings.data, filepath, concatdata);

            PrintLn("END");

            //Generate User reports to see what happened

            PrintLn("+---Report-----------------------------------------");
            for (int i = 0; i < concatdata.Count; i++)
            {
                PrintLn("| Device: " + myaio.devicenames[myaio.GetID(i)] + " (" + concatdata[i].Count + ")");
                PrintLn(GenerateDataReport(concatdata[i]));
            }
            PrintLn("+--------------------------------------------------");

            //SAVE LOG
            File.WriteAllText(settings.data.testpath + @"\log.txt", textBox1.Text);

            //Produce scope
            (new Scope(concatdata, settings.data.n_channels)).Show();

            SetStatus("Awaiting User Input...");

            //Now ask user input to save
            //Provide a freq -> complete header. Save header
            string fn = UserInputAfterSampling();
            RenameTempFile(fn);

            SetStatus("Ready");
        }

        void ProcessReturnValues(long ret)
        {
            if (ret != 0)
            {
                //There are errors to log
                foreach(string msg in myaio.error_log)
                    PrintLn(msg);
            }
        }

        string UserInputAfterSampling()
        {
            string filepath;
            //get default filename
            if (checkBox1.Checked)
            {
                filepath = IO.GetFilePathCal(settings.data, cbOrientation.SelectedIndex);
            }
            else
            {
                filepath = IO.GetFilePathTest(settings.data);
            }
            
            //User decides:
            //freq
            //filename
            using (UserCompleteTest testDialog = new UserCompleteTest(filepath))
            {
                if (testDialog.ShowDialog(this) == DialogResult.OK)
                {
                    settings.data.frequency = (float)testDialog.nudFreq.Value;
                    filepath = testDialog.tbFilename.Text;
                    PrintLn("Frequency: \t" + settings.data.frequency);
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
                    IO.MoveTempFile(settings, fn);
                    PrintLn("Saved: " + fn);
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

        void StartSampling()
        {
            if (settings.data.n_devices == 0)
            {
                SetStatus("Warning: No Devices connected. Reset");
                return;
            }
            
            List<int> failedID;
            if (!myaio.DeviceCheck(settings.data.n_channels, out failedID))
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
            long ret;
            ret = myaio.ResetTest();
            ProcessReturnValues(ret);

            var num_samples = nudDuration.Value * (decimal)1E6 / nudInterval.Value;

            PrintLn("----------------------------------------------------\r\nApplied Settings");
            PrintLn(settings.ToString());

            ret = myaio.SetupTimedSample(settings.data);
            ProcessReturnValues(ret);


            ret = myaio.Start((uint)this.Handle.ToInt32());
            ProcessReturnValues(ret);

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
                    settings.data.testpath = fbd.SelectedPath;
                    loadBindData();
                }
            }
        }





        /////////////////////////////////////////////////////////////////////////
        // GUI EVENTS
        /////////////////////////////////////////////////////////////////////////

        private void monitorChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Monitor(myaio, settings.data.n_channels)).Show();
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

        private void resetDevicesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            long ret = myaio.ResetDevices();
            ProcessReturnValues(ret);

            settings.data.n_devices = myaio.DiscoverDevices(DEVICE_ROOT);

            SetStatus(settings.data.n_devices + " Devices Connected");

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
            if (settings.data.path != "")
                settings.Save(settings.data.path);
            else
                saveSettings();
        }

        private void resetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            settings.Reload();
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

                    settings.Load(filename);

                    loadBindData();

                    displayPath(filename, settings.data.modified);

                    //So can reset
                    settings.data.path = filename;

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
                    settings.Save(filename);
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
            settings.Reload();
            loadBindData();
            displayPath(settings.data.path);
            has_loaded = true;
        }

        void displayPath(string path, bool modified = false)
        {

            string fn = IO.getFileName(path);
            string fn1 = fn + ((modified) ? " (modified)" : "");

            toolStripStatusLabel1.Text = "Current config: " + fn1;

            //System.Collections.Specialized.StringCollection sc = Properties.Settings.Default.processing_settings_prev;

        }

    }
}
