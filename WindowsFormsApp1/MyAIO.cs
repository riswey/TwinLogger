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

        const int DATA_RECEIVE_EVENT = 100;

        //static string device_root = "Aio00";
        const string LINEEND = "\r\n";

        //TODO: this will crash if not installed. Check
        Caio aio = new Caio();

        private List<DEVICEID> devices { get; } = new List<DEVICEID>();
        public Dictionary<DEVICEID, string> devicenames { get; } = new Dictionary<DEVICEID, string>();
        public Dictionary<DEVICEID, List<int[]>> data { get; } = new Dictionary<DEVICEID, List<int[]>>();

        //Handling device return values
        const long CLEAR_ERROR_LOG = -1;
        public List<string> error_log = new List<string>();
        private long elog
        {
            get
            {
                return error_log.Count;
            }
            set
            {
                Debug.WriteLine(AIOERRORCODES(value));

                if (value == CLEAR_ERROR_LOG)                                //set to -1 to clear log
                    error_log.Clear();
                else if (value == 0)
                    return;
                else
                    error_log.Add(AIOERRORCODES( value ) );
            }
        }

        public DEVICEID GetID(int idx)
        {
            if (idx < devices.Count)
            {
                return devices[idx];
            }
            return -1;
        }

        private int finished_count = 0;

        ~MyAIO()
        {
            Close();
        }

        public int DiscoverDevices(string device_root)        //add devices
        {
            //TODO: rather than looking at error code is there a proactive way to find devices? 

            DEVICEID id = 0;
            string full_device_name;

            devices.Clear();
            devicenames.Clear();

            for (int i = 0; i < 10; i++)
            {
                full_device_name = device_root + i;
                long ret = aio.Init(full_device_name, out id);
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

        public long ResetTest()
        {
            //Reset internal data store
            data.Clear();
            foreach(DEVICEID id in devices)
            {
                data[id] = new List<int[]>();
            }
            elog = CLEAR_ERROR_LOG;

            //Reset External Buffers
            foreach (DEVICEID id in devices)
            {
                elog = aio.ResetAiMemory(id);
            }
            return elog;
        }

        public bool DeviceCheck(short n_channels, out List<int> failedID)
        {
            failedID = new List<int>();

            //Per device
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
                    failedID.Add(GetID(i));
                }
            }

            return failedID.Count == 0;
        }

        public long ResetDevices()
        {
            elog = CLEAR_ERROR_LOG;
            //Resets Devices and Drivers
            foreach (DEVICEID id in devices)
            {
                elog = aio.ResetDevice(id);
            }
            return elog;
        }

        public long SetupTimedSample(SettingData settings)
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
            elog = CLEAR_ERROR_LOG;

            //Setting the 
            foreach (DEVICEID id in devices)
            {
                elog = aio.SetAiChannels(id, settings.n_channels);
                elog = aio.SetAiSamplingClock(id, settings.timer_interval);  //default usec (2000 for)
                elog = aio.SetAiStopTimes(id, settings.n_samples);
                elog = aio.SetAiEventSamplingTimes(id, DATA_RECEIVE_EVENT);        //#samples until data retrieve event

                elog = aio.SetAiTransferMode(id, 0);                //Device buffered 1=sent to user memory
                elog = aio.SetAiMemoryType(id, 0);                  //FIFO 1=Ring

                if (settings.external_trigger)
                {
                    elog = aio.SetAiClockType(id, 1);                   //external
                    elog = aio.SetAiStartTrigger(id, 1);                //1 by External trigger rising edge
                }
                else
                {
                    elog = aio.SetAiClockType(id, 0);                   //internal
                    elog = aio.SetAiStartTrigger(id, 0);                //0 by Software
                }

                elog = aio.SetAiStopTrigger(id, 0);                 //0 means by time
            }

            return elog;
        }

        public long Start(uint HandleMsgLoop)
        {
            finished_count = 0;

            elog = CLEAR_ERROR_LOG;

            foreach (DEVICEID id in devices)
            {
               //End, 500, set num events
                elog = aio.SetAiEvent(id, HandleMsgLoop, (int)(CaioConst.AIE_END | CaioConst.AIE_DATA_NUM | CaioConst.AIE_DATA_TSF));
            }

            foreach (DEVICEID id in devices)
            {
                elog = aio.StartAi(id);
            }
            return elog;
        }

        public long RetrieveData(DEVICEID device_id, int num_samples, int n_channels)
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

            elog = CLEAR_ERROR_LOG;

            int sampling_times = num_samples;

            int[] data1 = new int[n_channels * num_samples];

            elog = aio.GetAiSamplingData(device_id, ref sampling_times, ref data1);

            //if sampling times changes then sampling cut short

            if (elog == 0) {
                //store data
                data[device_id].Add( data1 );   
            }
            return elog;
        }

        public long DeviceFinished(short device_id, int num_samples, int n_channels)
        {
            finished_count++;

            elog = CLEAR_ERROR_LOG;

            elog = RetrieveData(device_id, num_samples, n_channels);

            return elog;
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
                aio.MultiAiEx(id, n_channels, aidata);
                snapshot.Add(aidata);
            }
            return snapshot;
        }

        public static float I2V(int bits)
        {
            return (float)bits / 65535 * 20 - 10;
        }

    }

}
