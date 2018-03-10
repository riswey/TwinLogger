using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

//Dllimport
using System.Runtime.InteropServices;
using System.Threading;

namespace MultiDeviceAIO
{
    public partial class Main : Form
    {
        MyMultiAIO myaio = new MyMultiAIO();

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

            myaio.DiscoverDevices("Aio00");
        }

        ~Main()
        {
            myaio.Close();
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

            myaio.SetupTimedSample((short)nudChannel.Value, (short)nudInterval.Value,(short)num_samples, CaioConst.P1);

            myaio.SetupExternalParameters(Double.Parse(tbFreq.Text), cbClips.Checked);

            print("START");

            myaio.Start( (uint)this.Handle.ToInt32() );

            setStatus("Sampling...");
            print("Sampling...");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            myaio.Stop();
            setStatus("Run stopped");
        }


        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {
                        short device_id = (short) m.WParam;
                        int num_samples = (int) m.LParam;

                        print("Finished " + device_id + "(" + num_samples + ")");
                        String fn = "";

                        try {
                            int ret = myaio.RetrieveData(device_id, num_samples);
                            print("Retrieved " + device_id + "(" + ret + ")");
                            fn = myaio.SaveData();
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
                        print("Processed... " + m.LParam);
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        void setStatus(string msg)
        {
            toolStripStatusLabel1.Text = msg;
        }

        void print(string msg)
        {
            textBox1.Text += msg + "\r\n";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Monitor m = new Monitor();
            m.device_name = myaio.devicenames[1];
            m.Show();
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

        private void timer1_Tick(object sender, EventArgs e)
        {

        }
    }

}
