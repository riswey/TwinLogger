using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaioCs;

using System.Diagnostics;

/// <summary>
/// Class wraps devices
/// State represents current device config (and nothing else!!!)
/// </summary>

/*
 * 
 * 
 *  devices need to be reset
 * 
 * assume all devices data gets prepared.
 * 
 * when last id gets called call the save data
 * 
 * NOTE: am using a counter on async returns! Possible fail point!
 * 
 */

using DEVICEID = System.Int16;
//The data structure is a dictionary; K: device id V:raw list of data for device
//K: device id :. data imported by id
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;

namespace MultiDeviceAIO
{
    public class MyAIO
    {

        const int DATA_RECEIVE_EVENT = 100;

        //static string device_root = "Aio00";
        const string LINEEND = "\r\n";

        //TODO: this will crash if not installed. Check
        public Caio aio;

        private List<DEVICEID> devices { get; } = new List<DEVICEID>();
        public Dictionary<DEVICEID, string> devicenames { get; } = new Dictionary<DEVICEID, string>();

        /// <summary>
        /// Data arrives in int[] per device
        /// A list is built of these arrays for each device (async events per device)
        /// </summary>
        public DATA data { get; private set; } = new DATA();

        public DEVICEID GetID(int idx)
        {
            if (idx < devices.Count)
            {
                return devices[idx];
            }
            return -1;
        }

        private int finished_count = 0;

        public MyAIO(bool testing)
        {
            if (testing)
                aio = new Caio1();
            else
                aio = new Caio();
        }

        ~MyAIO()
        {
            Close();
        }

        long HANDLE_RETURN_VALUES
        {
            set
            {
                if (value != 0)
                {
                    throw new AIODeviceException(value);
                }
            }
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
                    data[id] = new List<int>();
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

        public void ResetTest()
        {
            //Reset internal data store
            data.Clear();
            foreach (DEVICEID id in devices)
            {
                data[id] = new List<int>();
            }

            //Reset External Buffers
            foreach (DEVICEID id in devices)
            {
                HANDLE_RETURN_VALUES = aio.ResetAiMemory(id);
            }
        }

        public bool DeviceCheck(short n_channels, out List<int> failedID)
        {
            //Check that devices have values

            failedID = new List<int>();

            //Per device
            List<float[]> snapshot = ChannelsSnapShot(n_channels);

            float[] sum = new float[snapshot.Count];

            for (int i = 0; i < snapshot.Count; i++)
            {
                sum[i] = 0;
                foreach (float f in snapshot[i])
                {
                    sum[i] += f * f;
                }
            }

            for (int i = 0; i < sum.Length; i++)
            {
                if (sum[i] == 0)
                {
                    failedID.Add(GetID(i));
                }
            }

            return failedID.Count == 0;
        }

        public void ResetDevices()
        {
            //Resets Devices and Drivers
            foreach (DEVICEID id in devices)
            {
                HANDLE_RETURN_VALUES = aio.ResetDevice(id);
            }
        }

        public void SetupTimedSample(LoggerState settings)
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

            //Setting the 
            foreach (DEVICEID id in devices)
            {
                HANDLE_RETURN_VALUES = aio.SetAiChannels(id, settings.n_channels);
                HANDLE_RETURN_VALUES = aio.SetAiSamplingClock(id, settings.timer_interval);  //default usec (2000 for)
                HANDLE_RETURN_VALUES = aio.SetAiStopTimes(id, settings.n_samples);
                HANDLE_RETURN_VALUES = aio.SetAiEventSamplingTimes(id, 500);        //#samples until data retrieve event

                HANDLE_RETURN_VALUES = aio.SetAiTransferMode(id, 0);                //Device buffered 1=sent to user memory
                HANDLE_RETURN_VALUES = aio.SetAiMemoryType(id, 0);                  //FIFO 1=Ring

                if (settings.external_control)
                {
                    HANDLE_RETURN_VALUES = aio.SetAiStartTrigger(id, 1);                //1 by External trigger rising edge
                }
                else
                {
                    HANDLE_RETURN_VALUES = aio.SetAiStartTrigger(id, 0);                //0 by Software
                }

                if (settings.external_control)
                {
                    HANDLE_RETURN_VALUES = aio.SetAiClockType(id, 1);                   //external
                }
                else
                {
                    HANDLE_RETURN_VALUES = aio.SetAiClockType(id, 0);                   //internal
                }

                HANDLE_RETURN_VALUES = aio.SetAiStopTrigger(id, 0);                     //0 means by time

            }
        }

        public void Start(uint HandleMsgLoop)
        {
            finished_count = 0;
            
            foreach (DEVICEID id in devices)
            {
                //End, 500, set num events
                HANDLE_RETURN_VALUES = aio.SetAiEvent(id, HandleMsgLoop, (int)(CaioConst.AIE_END | CaioConst.AIE_DATA_NUM | CaioConst.AIE_DATA_TSF));
            }

            foreach (DEVICEID id in devices)
            {
                HANDLE_RETURN_VALUES = aio.StartAi(id);
            }
        }

        //DEBUG
        public Caio getCaio()
        {
            return aio;
        }

        public void Stop()
        {
            foreach (DEVICEID id in devices)
            {
                aio.StopAi(id);
            }
        }

        public void RetrieveData(DEVICEID device_id, int num_samples, int n_channels)
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

            HANDLE_RETURN_VALUES = aio.GetAiSamplingData(device_id, ref sampling_times, ref data1);

            //NOTE: if sampling times changes then sampling cut short

            //store data
            data[device_id].AddRange(data1);
        }

        public void DeviceFinished(short device_id) { finished_count++; }
        public bool IsTestFinished() { return finished_count == devices.Count; }

        /// <summary>
        /// device lists of int[] are concatenated ->
        /// DeviceX => int[] (index 0)
        /// DeviceY => int[] (index 1)
        /// ...
        /// </summary>
        /// <param name="concatdata"></param>
        /// <returns></returns>
        public bool GetData(out DATA concatdata)
        {
            //TODO: return ref
            if (!IsTestFinished())
            {
                concatdata = new DATA();
                return false;
            }

            concatdata = data;
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

        public List<int[]> ChannelsSnapShotBinary(short n_channels)
        {
            List<int[]> snapshot = new List<int[]>();
            foreach (DEVICEID id in devices)
            {
                int[] aidata = new int[n_channels];
                aio.MultiAi(id, n_channels, aidata);
                snapshot.Add(aidata);
            }
            return snapshot;
        }


        public static float I2V(int bits)
        {
            return (float)bits / 65535 * 20 - 10;
        }

        public void LoadData(ref DATA data) {
            this.data = data;
        }

    }

}
