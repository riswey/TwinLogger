using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics;

using CaioCs;

/*
 * Num Samples is channels * duration / sampling_freq !!!
 * (1) Get float + bin to agree!
 * (2)Test clearing and taking data again
 * 
 * 
 * 
 */


namespace MultiDeviceAIO
{
    public partial class TEST : Form
    {
        string device_name = "Aio000";
        Caio aio = new Caio();
        short id;

        short num_channels = 4 * 3;
        //int duration = 100;
        int num_samples = 100 * 12;

        /*
         *  Appears that get data retrieves the num of samples = channels * duration!
         */

        public TEST()
        {
            InitializeComponent();

            int ret = aio.Init(device_name, out id);

            if (ret == 0)
            {
                WriteLine("Initialised");
            }
            else
            {
                WriteLine("Failed to Init");
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Init

            WriteLine("Init");

            int ret = SetupTimedSample();
            WriteLine("Setup : " + ret.ToString());

            //| CaioConst.AIE_DATA_NUM
            ret = aio.SetAiEvent(id, (uint)this.Handle.ToInt32(), (int)(CaioConst.AIE_END));
            WriteLine("Set Event Handler : " + ret.ToString());

            //Start timed test
            ret = aio.StartAi(id);
            WriteLine("Start =" + ret);

        }

        void GotData(int n_samples, bool infloat)
        {
            WriteLine(n_samples + "==" + num_samples);

            int sampling_times = n_samples;

            int ret;
            object alldata;

            if (infloat) {
                WriteLine("Getting Float");
                float[] data = new float[num_samples];
                ret = aio.GetAiSamplingDataEx(id, ref sampling_times, ref data);
                alldata = data;
            }
            else
            {
                WriteLine("Getting Binary");
                int[] data = new int[num_samples];
                ret = aio.GetAiSamplingData(id, ref sampling_times, ref data);
                alldata = data;
            }

            WriteLine("Ret: " + ret.ToString());

            WriteLine("Sampling Times: " + sampling_times.ToString());

            return;

            switch (ret)
            {
                case 21584:
                    WriteLine("FIFO empty");
                    break;
                case 21580:
                    WriteLine("It tried to acquire data beyond the converted number");
                    break;
            }

            string value;
            for(int i=0;i< num_channels; i++)
            {
                float num = ((float[])alldata)[i];

                if (infloat)
                {
                    value = num.ToString();
                } else
                {
                    value = (num / 65535 * 20 - 10).ToString();
                }

                Write(value + "\t");
                if (i % 3 == 2) Write("\r\n");
            }
        }

        public int SetupTimedSample()
        {

            int ret = aio.SetAiChannels(id, num_channels);
            ret += aio.SetAiStopTimes(id, num_samples);
            ret += aio.SetAiSamplingClock(id, 1000);            //default usec (2000 for)

            ret += aio.SetAiTransferMode(id, 0);                //Device buffered 1=sent to user memory
            ret += aio.SetAiMemoryType(id, 0);                  //FIFO 1=Ring
            ret += aio.SetAiClockType(id, 0);                   //internal
            ret += aio.SetAiStartTrigger(id, 0);                //0 means by software
            ret += aio.SetAiStopTrigger(id, 0);                 //0 means by time

            //ret += aio.SetAiEventSamplingTimes(id, 100);        //#samples until asks data retrieve

            ret += aio.ResetAiMemory(id);

            return ret;
        }
        
        void WriteLine(string str)
        {
            textBox1.Text += str + "\r\n";
        }

        void Write(string str)
        {
            textBox1.Text += str;
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {
                        short device_id = (short)m.WParam;
                        int num_samples = (int)m.LParam;

                        //WriteLine("Getting Data ... " + num_samples);

                        int sampling_times = num_samples;
                        int[] data = new int[num_samples];

                        int ret = aio.GetAiSamplingData(id, ref sampling_times, ref data);

                        //WriteLine("SamplingData: " + ret);

                        //GotData(num_samples, false);

                        //WriteLine("Ended");

                    }
                    break;
                case 0x1003:
                    {

                        short device_id = (short)m.WParam;
                        int num_samples = (int)m.LParam;

                        WriteLine("Buffer...");
                        //buffer fill event
                    }
                    break;
            }

            base.WndProc(ref m);
        }



    }
}
