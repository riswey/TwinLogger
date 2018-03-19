using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaioCs;

using System.Diagnostics;

/*
 *  devices need to be reset
 * 
 * assume all devices data gets prepared.
 * 
 * when last id gets called call the save data
 * 
 * NOTE: am using a counter on async returns! Possible fail point!
 * 
 */


namespace MultiDeviceAIO
{
    using DEVICEID = System.Int16;

    public class MyAIO
    {

        const int DATA_RECEIVE_EVENT = 50;

        //static string device_root = "Aio00";
        const string LINEEND = "\r\n";

        //TODO: this will crash if not installed. Check
        Caio aio = new Caio();

        public List<DEVICEID> devices { get; } = new List<DEVICEID>();
        public Dictionary<DEVICEID, string> devicenames { get; } = new Dictionary<DEVICEID, string>();
        public Dictionary<DEVICEID, List<int[]>> data { get; } = new Dictionary<DEVICEID, List<int[]>>();

        private int finished_count = 0;

        ~MyAIO()
        {
            Close();
        }

        public int DiscoverDevices(string device_root)        //add devices
        {
            long ret = 1;
            DEVICEID id = 0;
            string full_device_name;

            devices.Clear();
            devicenames.Clear();

            for (int i = 0; i < 10; i++)
            {
                full_device_name = device_root + i;
                ret = aio.Init(full_device_name, out id);
                if (ret == 0)
                {
                    devices.Add(id);
                    devicenames[id] = full_device_name;
                    data[id] = new List<int[]>();
                }
            }

            return devices.Count();
        }

        public void Close()
            {
            foreach (DEVICEID id in devices)
            {
                aio.Exit(id);
            }
            devices.Clear();
        }

        public void Reset()
        {
            //Reset internal data store
            data.Clear();
            foreach(DEVICEID id in devices)
            {
                data[id] = new List<int[]>();
            }
            //Reset External Buffers
            foreach (DEVICEID id in devices)
            {
                aio.ResetAiMemory(id);
            }
        }

        public bool DeviceCheck(short n_channels, out List<int> failed)
        {
            failed = new List<int>();

            List<float[]> snapshot = ChannelsSnapShot(n_channels);

            float[] sum = new float[snapshot.Count];

            for(int i=0; i<snapshot.Count; i++)
            {
                sum[i] = 0;
                foreach (float f in snapshot[i])
                {
                    sum[i] += f * f;
                }
            }

            for(int i=0; i < sum.Length; i++)
            {
                if (sum[i] == 0)
                {
                    failed.Add(i);
                }
            }

            return failed.Count == 0;
        }

        public void ResetDevices()
        {
            //Resets Devices and Drivers
            foreach (DEVICEID id in devices)
            {
                aio.ResetDevice(id);
            }
        }

        public int SetupTimedSample(I_TestSettings settings)
        {
            /*
            //resolution (works) (12/16bit)
            aio.GetAiResolution(id, out AiResolution);
            maxbytes = Math.Pow(2, AiResolution);

            //Doesn't work on 1664LAX
            aio.SetAiRangeAll(1,CaioConst.P025)
            
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

            int ret = 0;
            //Setting the 
            foreach (DEVICEID id in devices)
            {
                ret += aio.SetAiChannels(id, settings.n_channels);
                ret += aio.SetAiSamplingClock(id, settings.timer_interval);  //default usec (2000 for)
                ret += aio.SetAiStopTimes(id, settings.n_samples);
                ret += aio.SetAiEventSamplingTimes(id, DATA_RECEIVE_EVENT);        //#samples until data retrieve event

                ret += aio.SetAiTransferMode(id, 0);                //Device buffered 1=sent to user memory
                ret += aio.SetAiMemoryType(id, 0);                  //FIFO 1=Ring
                ret += aio.SetAiClockType(id, 0);                   //internal
                ret += aio.SetAiStartTrigger(id, 0);                //0 means by software
                ret += aio.SetAiStopTrigger(id, 0);                 //0 means by time
            }
            return ret;
        }

        public int Start(uint HandleMsgLoop)
        {
            finished_count = 0;

            int ret = 0;

            foreach (DEVICEID id in devices)
            {
               //End, 500, set num events
                ret += aio.SetAiEvent(id, HandleMsgLoop, (int)(CaioConst.AIE_END | CaioConst.AIE_DATA_NUM | CaioConst.AIE_DATA_TSF));
            }

            foreach (DEVICEID id in devices)
            {
                ret += aio.StartAi(id);
            }
            return ret;
        }

        public int RetrieveData(DEVICEID device_id, int num_samples, int n_channels)
        {
            /* Testing
             * =======
             * 
             * Confirmed that data size is correct: n_samples x n_channels 
             * 
             * Confirmed that data is zeroed before loading
             * e.g. inc. data array and unfilled data is 0
             * 
             * You cannot get false data reading.
             */


            int sampling_times = num_samples;

            int[] data1 = new int[n_channels * num_samples];

            int ret = aio.GetAiSamplingData(device_id, ref sampling_times, ref data1);

            //if sampling times changes then sampling cut short

            if (ret == 0) {
                //store data
                data[device_id].Add( data1 );   
            }
            return ret;
        }

        public int DeviceFinished(short device_id, int num_samples, int n_channels)
        {
            finished_count++;
            return RetrieveData(device_id, num_samples, n_channels);
        }

        public bool IsTestFinished()
        {
            return finished_count == devices.Count;
        }

        public bool GetData(out List<List<int>> concatdata)
        {
            concatdata = new List<List<int>>();

            if (!IsTestFinished()) return false;
            //Concat data
            foreach (DEVICEID id in devices)
            {
                List<int> device_data = new List<int>();
                foreach (int[] bundle in data[id])
                {
                    device_data.AddRange(bundle);
                }
                concatdata.Add(device_data);
            }

            return true;
        }

        public List<float[]> ChannelsSnapShot(short n_channels)
        {
            List<float[]> snapshot = new List<float[]>();
            foreach (DEVICEID id in devices)
            {
                float[] aidata = new float[n_channels];
                long ret = aio.MultiAiEx(devices[id], n_channels, aidata);
                snapshot.Add(aidata);
            }
            return snapshot;
        }
        /*
        public Tuple<bool, string, string> SmartCalibration(short n_channels)
        {
            //TODO: 

            //Get snapshot
            List<float[]> ss = ChannelsSnapShot(n_channels);

            //Examine.
            foreach (float[] chss in ss)
            {
                foreach(float volt in chss)
                {
                    //Find orientation
                    //lowest/highest + 2 "similar"
                    //otherwise failed
                    //Genaret report
                }
            }


            //if OK
            //sample
            //save
            
            
            //return report

            return new Tuple<bool, string, string>(false, "ok", "ok");
        }
        */
        public static float I2V(int bits)
        {
            return (float)bits / 65535 * 20 - 10;
        }

    }

}
