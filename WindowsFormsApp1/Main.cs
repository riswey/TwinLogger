using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

//Dllimport
using System.Runtime.InteropServices;
using System.Threading;
using System.Media;

using System.Diagnostics;
using System.IO;

namespace MultiDeviceAIO
{
    public partial class Main : Form
    {
        MyAIO myaio;
        AIOSettings settings = new AIOSettings();

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
            myaio = new MyAIO(settings.data);
            myaio.DiscoverDevices("Aio00");

        }

        ~Main()
        {
            myaio.Close();
        }

        void loadBindData()
        {
            cbMass.DataBindings.Clear();
            cbMass.DataBindings.Add("SelectedIndex", settings.data, "mass");

            cbClips.DataBindings.Clear();
            cbClips.DataBindings.Add("Checked", settings.data, "clipsOn");

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

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {
                        myaio.DeviceFinished();

                        short device_id = (short) m.WParam;
                        int num_samples = (int) m.LParam;

                        print("Finished " + device_id + "(" + num_samples + ")");
                        String fn = "";

                        try {
                            int ret = myaio.RetrieveData(device_id, num_samples);
                            print("Retrieved " + device_id + "(" + ret + ")");

                            if (myaio.TestFinished())
                            {
                                GetFreq testDialog = new GetFreq();

                                // Show testDialog as a modal dialog and determine if DialogResult = OK.
                                if (testDialog.ShowDialog(this) == DialogResult.OK)
                                {
                                    settings.data.frequency = Int32.Parse(testDialog.textBox1.Text);
                                }

                                testDialog.Dispose();

                                fn = myaio.SaveData();
                            }

                        }
                        catch (Exception e)
                        {
                            setStatus("Not Saved: " + e.Message);
                        }

                        if (fn != null)
                        {
                            setStatus("Saved: " + fn);
                            print("Saved: " + fn);

                        }
                    }
                    break;
                case 0x1003:
                    {
                        short device_id = (short)m.WParam;
                        int num_samples = (int)m.LParam;
                        int ret = myaio.RetrieveData(device_id, num_samples);
                        print(".", false);
                    }
                    break;
            }

            base.WndProc(ref m);
        }



        void setStatus(string msg)
        {
            toolStripStatusLabel1.Text = msg;
        }

        void print(string msg, bool linebreak = true)
        {
            textBox1.Text += msg + (linebreak?"\r\n":"");
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

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

        private void monitorChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Monitor m = new Monitor();
            m.aio = myaio;
            m.Show();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int ret;

            /*
                        //resolution (works)
                        aio.GetAiResolution(id, out AiResolution);
                        maxbytes = Math.Pow(2, AiResolution);

                        //Doesn't work (reads 1)
                        short nC1;
                        aio.GetAiChannels(id, out nC1);

                        //is this a mapping?
                        string map = "";
                        AiChannelSeq = new short[nChannel];
                        for (short i = 0; i < nChannel; i++)
                        {
                            aio.GetAiChannelSequence(id, i, out AiChannelSeq[i]);
                            map += AiChannelSeq[i].ToString() + ",";
                        }
                        */
            var num_samples = nudDuration.Value * (decimal)1E6 / nudInterval.Value;

            myaio.SetupTimedSample((short)nudChannel.Value, (short)nudInterval.Value, (short)num_samples, CaioConst.P1);

            print("START");

            myaio.Start((uint)this.Handle.ToInt32());

            setStatus("Sampling...");

            print("Sampling", false);

        }


        private void button2_Click(object sender, EventArgs e)
        {
            myaio.Stop();
            print("STOP");
            setStatus("Run stopped");
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadSettings();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (settings.data.path != "")
                settings.Save(settings.data.path);
            else
                saveSettings();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.Reload();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SelectDirectory();
        }


        ////////////////////////////////////////////////////////////////////////////////////
        // CODE FROM SETTINGS
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

        static string getFileName(string path)
        {
            char divider = '\\';
            if (path.IndexOf('/') > 0) divider = '/';

            string[] paths = path.Split(divider);

            return paths[paths.Length - 1];
        }

        void displayPath(string path, bool modified = false)
        {

            string fn = getFileName(path);
            string fn1 = fn + ((modified) ? " (modified)" : "");

            toolStripStatusLabel1.Text = "Current config: " + fn1;

            //System.Collections.Specialized.StringCollection sc = Properties.Settings.Default.processing_settings_prev;

        }

        private void selectDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectDirectory();
        }

        private void SelectDirectory()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {

                    settings.data.testpath = fbd.SelectedPath;
                    loadBindData();

                }
            }
        }

    }

}
