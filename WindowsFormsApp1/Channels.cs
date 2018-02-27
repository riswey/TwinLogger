using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Channels : Form
    {
        public List<MyAIO> devices;

        public Channels()
        {
            InitializeComponent();
            devices = MyAIO.GetDeviceList();
        }

        ~Channels()
        {
            MyAIO.CloseDeviceList(devices);
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

            devices[0].SetupTimedSample((short)nudChannel.Value, (short)nudInterval.Value,(short)num_samples, CaioConst.P1);

            devices[0].SetupExternalParameters(Double.Parse(tbFreq.Text), cbClips.Checked);

            print("START");

            devices[0].Start( (uint)this.Handle.ToInt32() );

            setStatus("Sampling...");
            print("Sampling...");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            devices[0].Stop();
            setStatus("Run stopped");
        }


        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {
                        int call_id = (Int16) m.WParam;
                        int num_samples = (int) m.LParam;

                        try {
                            string path = MyAIO.findDevice(devices, call_id).Save(num_samples);
                            setStatus("Saved: " + path);
                            print("Saved to: " + path);
                        }
                        catch (Exception e)
                        {
                            setStatus("Not Saved: " + e.Message);
                        }
                    }                    
                    break;
                case 0x1003:
                    {
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
            m.device_name = "Aio001";
            m.Show();
        }
    }

}
