using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaioCs;
/*
 * 
 * Wrapper for Caio in case it is only designed for one device.
 * 
 */

namespace WindowsFormsApp1
{
    public class MyAIO
    {
        static string device_root = "Aio00";

        Caio aio = new Caio();
        short id = 0;

        private int device_number { get; set; }
        bool bound = false;         //check that bound to a device

        public short n_channels { get; set; }
        public short timer_interval { get; set; }
        public short range { get; set; }
        public short n_samples { get; set; }

        //External parameters
        public float frequency { get; set; }
        public bool clipson { get; set; }


        static public List<MyAIO> GetDeviceList()
        {
            Caio aio = new Caio();

            List<MyAIO> devices = new List<MyAIO>();

            long ret = 1;
            short id1 = 0;
            string device_name;
            for (int i = 0; i < 10; i++)
            {
                device_name = device_root + i;
                ret = aio.Init(device_name, out id1);
                if (ret == 0)
                {
                    devices.Add( new MyAIO(i) );
                }
            }

            return devices;
        }

        static public void CloseDeviceList(List<MyAIO> devices)
        {
            foreach(MyAIO myaio in devices)
            {
                myaio.Close();
            }
        }

        public MyAIO(int number)
        {
            device_number = number;
            string device_name = device_root + device_number;
            int ret = aio.Init(device_name, out id);
            if (ret == 0)
            {
                bound = true;
            }
        }

        public void Close()
        {
            aio.Exit(id);
        }

        public int setTimedSample(short n_channels, short timer_interval, short n_samples, CaioConst range)
        {
            this.n_channels = n_channels;
            this.timer_interval = timer_interval;
            this.n_samples = n_samples;
            this.range = (short) range;

            int ret = aio.SetAiChannels(id, this.n_channels);
            ret += aio.SetAiTransferMode(id, 0);                //Device buffered 1=sent to user memory
            ret += aio.SetAiMemoryType(id, 0);                  //FIFO 1=Ring
            ret += aio.SetAiClockType(id, 0);                   //internal
            ret += aio.SetAiSamplingClock(id, this.timer_interval);  //default usec (2000 for)
            ret += aio.SetAiRangeAll(id, this.range);
            ret += aio.SetAiStartTrigger(id, 0);                //0 means by software
            ret += aio.SetAiStopTrigger(id, 0);                 //0 means by time
            ret += aio.SetAiStopTimes(id, this.n_samples);          
            ret += aio.ResetAiMemory(id);
            return ret;
        }

        public int start(uint HandleMsgLoop)
        {
            int ret;
            ret = aio.SetAiEvent(id, HandleMsgLoop, (int)(CaioConst.AIE_END | CaioConst.AIE_DATA_NUM));
            ret += aio.StartAi(id);
            return ret;
        }

        public bool save()
        {
            int sampling_times = n_samples;
            int[] data = new int[this.n_channels * this.n_samples];
            int ret = aio.GetAiSamplingData(id, ref sampling_times, ref data);

            string header = "Device," + device_number + "\nChannels," + n_channels + "\nInterval," + timer_interval + "us\nSamples," + n_samples;

            string path = "";
            string filename = (new DateTime()).ToShortDateString() + this.device_number + this.frequency + (this.clipson?"ON":"OFF") + n_channels + n_samples + ".csv";

            string str;
            try
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(path + filename))
                {
                    file.WriteLine(header);

                    for (int n = 0; n < n_samples; n++)
                    {
                        str = data[n].ToString();
                        for (int c = 1; c < n_channels; c++)
                        {
                            str += "," + data[c * n_samples + n].ToString();
                        }
                        file.WriteLine(str);
                    }
                    file.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }

        }

    }
}
