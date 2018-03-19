using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MultiDeviceAIO
{
    public partial class Calibration : Form
    {
        MyAIO myaio;

        public Calibration(MyAIO aio)
        {
            InitializeComponent();
            this.myaio = aio;
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {

                        /*                        short device_id = (short)m.WParam;
                                                int num_samples = (int)m.LParam;

                                                myaio.DeviceFinished();

                                                print("Finished " + device_id + "(" + num_samples + ")");
                                                String fn = "";

                                                try
                                                {
                                                    int ret = myaio.RetrieveData(device_id, num_samples);
                                                    print("Retrieved " + device_id + "(" + ret + ")");

                                                    if (myaio.IsTestFinished())
                                                    {
                                                        UserCompleteTest testDialog = new UserCompleteTest();

                                                        //TODO
                                                        fn = "CHANGGETHIS";

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
                                            */
                    }
                    break;
                case 0x1003:
                    {
                        short device_id = (short)m.WParam;
                        int num_samples = (int)m.LParam;
                        //int ret = myaio.RetrieveData(device_id, num_samples, n_channels);
                        print(".", false);
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        void print(string msg, bool linebreak = true)
        {
            textBox1.Text += msg + (linebreak ? "\r\n" : "");
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        void setStatus(string msg)
        {
            //toolStripStatusLabel1.Text = msg;
        }

    }


}
