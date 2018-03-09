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

namespace MultiDeviceAIO
{
    public class MyAIO: I_MyAIO
    {
        short n_channels { get; set; }
        short timer_interval { get; set; }
        short range { get; set; }
        short n_samples { get; set; }

        //External parameters
        public float frequency { get; set; }
        public bool clipsOn { get; set; }

        //Data counter
        int counter = 0;
        private int[] data;
        public bool data_ready = false;

        public Caio aio;
        public short id;
        public string device_name;

        bool bound = false;         //check that bound to a device

        public MyAIO()
        {
            aio = new Caio();
        }

        public void Init(string device_name)
        {
            this.device_name = device_name;
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

        public void SetupExternalParameters(double frequency, bool clipsOn)
        {
            this.frequency = (float)frequency;
            this.clipsOn = clipsOn;
        }

        public int SetupTimedSample(short n_channels, short timer_interval, short n_samples, CaioConst range)
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

        public int Start(uint HandleMsgLoop)
        {
            int ret;
            ret = aio.SetAiEvent(id, HandleMsgLoop, (int)(CaioConst.AIE_END | CaioConst.AIE_DATA_NUM));
            ret += aio.StartAi(id);
            return ret;
        }

        public void Stop()
        {
            aio.StopAi(id);
            aio.ResetAiMemory(id);
        }


        public void PrepareData(int device_id, int num_samples)
        {
            if (device_id != this.id || num_samples != this.n_samples)
            {
                throw new Exception("Sampling not complete.");
            }

            int sampling_times = num_samples;
            data = new int[this.n_channels * this.n_samples];
            int ret = aio.GetAiSamplingData(id, ref sampling_times, ref data);

            if (ret != 0)
            {
                throw new Exception("Sampling failed");
            }

            data_ready = true;
        }

        public string GetHeader(string delimiter)
        {
            return "";
        }

        public void Print(string delimiter, ref string visitor)
        {
            int start = counter * this.n_channels;
            int end = start + this.n_channels;


            if (end > this.n_channels * this.n_samples)
            {
                visitor = null;
            }
            else
            {
                //Add to existing
                if (visitor.Length > 0)
                    visitor += delimiter;

                List<string> str = new List<string>();
                for (int i = start + 1; i < end; i++)
                {
                    str.Add(data[i].ToString());
                }
                visitor += string.Join(delimiter, str);
            }
        }

        public void Reset()
        {
            aio.ResetAiMemory(id);
            counter = 0;
            data = null;
            data_ready = false;

            id = 0;
            device_name = null;

            bound = false;         //check that bound to a device
    }
}
}
