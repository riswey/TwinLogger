using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaioCs;

using System.Diagnostics;

/// <summary>
/// Class wraps Device.devices
/// State represents current device config (and nothing else!!!)
/// </summary>

/*
 * 
 * 
 *  Device.devices need to be reset
 * 
 * assume all Device.devices data gets prepared.
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
    public class Device
    {
        public static List<Device> devices = new List<Device>();

        public short id { get; set; }
        string name;
        public List<int> data = new List<int>();
        public int target { get; set; } = 0;
        public bool IsFinished { get; private set; } = false;
        public bool IsFailed { get; private set; } = false;
        //timer_duration / 1000 * sample_freq * num_channels
        public int[] buffer;

        public Device(short id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public void Clear()
        {
            data.Clear();
            IsFinished = IsFailed = false;
        }

        public int Add(int datapointcount, ref int[] buf)
        {
            if (datapointcount != 0)
            {
                var newArray = new int[datapointcount];
                Array.Copy(buf, newArray, datapointcount);
                data.AddRange(newArray);
            }
            else
            {
                IsFinished = true;
                if (data.Count != target)
                {
                    IsFailed = true;
                }
            }
            return data.Count;
        }

    }

    public class MyAIO
    {
        public const int TIMERPERIOD = 500;

        //static string device_root = "Aio00";
        const string LINEEND = "\r\n";

        //TODO: this will crash if not installed. Check
        public Caio aio;

        readonly Dictionary<CaioConst, string> AIOSTATUS = new Dictionary<CaioConst, string>() {
            {0, "Fuck Yeah" },
            {CaioConst.AIS_BUSY, "Device is running"},
            {CaioConst.AIS_START_TRG, "Wait the start trigger"},
            {CaioConst.AIS_DATA_NUM, "Store up to the specified number of data"},
            {CaioConst.AIS_OFERR, "Overflow"},
            {CaioConst.AIS_SCERR, "Sampling clock error"},
            {CaioConst.AIS_AIERR, "AD conversion error"},
            {CaioConst.AIS_DRVERR, "Driver spec error"}
        };

        public DATA concatdata { get; private set; } = null;      //Keep for Scope

        int[] buf = new int[100000];

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

        public long HANDLE_RETURN_VALUES
        {
            set
            {
                if (value != 0)
                {
                    aio.GetErrorString((int)value, out string ErrorString);
                    throw new Exception(ErrorString);
                }
            }
        }

        public int DiscoverDevices(string device_root)        //add Device.devices
        {
            //TODO: rather than looking at error code is there a proactive way to find Device.devices? 

            DEVICEID id = 0;
            string name;

            Device.devices.Clear();

            for (int i = 0; i < 10; i++)
            {
                name = device_root + i;
                long ret = aio.Init(name, out id);
                if (ret == 0)
                {
                    Device.devices.Add(new Device(id, name));
                }
            }

            return Device.devices.Count();
        }

        public void Close()
        {

            foreach (Device d in Device.devices)
            {
                aio.Exit(d.id);
            }
            Device.devices.Clear();
        }

        public void ResetTest()
        {
            foreach (Device d in Device.devices)
            {
                d.Clear();
            }

            //Reset External Buffers
            foreach (Device d in Device.devices)
            {
                HANDLE_RETURN_VALUES = aio.ResetAiMemory(d.id);
            }
        }

        public bool DeviceCheck()
        {
            foreach(Device d in Device.devices)
            {
                if (GetStatus(d.id) == null)
                {
                    return false;
                }
            }
            return true;
            /*
            //Check that Device.devices have values

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
            */
        }

        public void ResetDevices()
        {
            //Resets Device.devices and Drivers
            foreach (Device d in Device.devices)
            {
                HANDLE_RETURN_VALUES = aio.ResetDevice(d.id);
            }
        }

        public string GetStatus(DEVICEID id)
        {
            HANDLE_RETURN_VALUES = aio.GetAiStatus(id, out int AiStatus);

            if (AIOSTATUS.ContainsKey((CaioConst)AiStatus))
            {
                AIOSTATUS.TryGetValue((CaioConst)AiStatus, out string status);
                return status;
            }
            return AiStatus.ToString();
        }

        public string GetStatusAll()
        {
            string output = "";
            foreach (Device d in Device.devices)
            {
                output += String.Format("Status{0}: {1}\r\n", d.id, GetStatus(d.id));
            }
            return output;
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
            foreach (Device d in Device.devices)
            {
                HANDLE_RETURN_VALUES = aio.SetAiChannels(d.id, settings.n_channels);
                HANDLE_RETURN_VALUES = aio.SetAiSamplingClock(d.id, settings.timer_interval);  //default usec (2000 for)
                HANDLE_RETURN_VALUES = aio.SetAiStopTimes(d.id, settings.n_samples);

                HANDLE_RETURN_VALUES = aio.SetAiMemoryType(d.id, 0);                      //FIFO 1=Ring

                HANDLE_RETURN_VALUES = aio.SetAiStopTrigger(d.id, 0);                     //0 means by time

                if (settings.external_control)
                {
                    HANDLE_RETURN_VALUES = aio.SetAiStartTrigger(d.id, 1);                //1 by External trigger rising edge
                }
                else
                {
                    HANDLE_RETURN_VALUES = aio.SetAiStartTrigger(d.id, 0);                //0 by Software
                }

                if (settings.external_control)
                {
                    HANDLE_RETURN_VALUES = aio.SetAiClockType(d.id, 1);                   //external
                }
                else
                {
                    HANDLE_RETURN_VALUES = aio.SetAiClockType(d.id, 0);                   //internal
                }

                //This is the expected number of samples per device
                d.target = settings.sample_frequency * settings.duration * settings.n_channels;

                //timer_duration / 1000 * sample_freq * num_channels *2 (no chance of overflow!)
                d.buffer = new int[TIMERPERIOD / 1000 * settings.sample_frequency * settings.n_channels * 2];

            }

            //Reset object state
            concatdata = null;

        }

        public void Start()
        {
            foreach (Device d in Device.devices)
            {
                HANDLE_RETURN_VALUES = aio.StartAi(d.id);
            }
        }       

        public void Stop()
        {
            foreach (Device d in Device.devices)
            {
                aio.StopAi(d.id);
            }
        }


        public int RetrieveAllData()
        {
            int t = 0;
            foreach (Device d in Device.devices)
            {
                t += RetrieveData(d);
            }
            return t;
        }

        public int RetrieveData(Device d)
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
            //if (num_samples > 0)
            //{
            int sampling_count;

            HANDLE_RETURN_VALUES = aio.GetAiSamplingCount(d.id, out sampling_count);


            if (sampling_count > 0)
            {
                HANDLE_RETURN_VALUES = aio.GetAiSamplingData(d.id, ref sampling_count, ref buf);
            }

            int size = d.Add(sampling_count * PersistentLoggerState.ps.data.n_channels, ref buf);

            //return sampling_count;
            return size;
        }

        public bool IsTestFinished {
            get
            {
                foreach (Device d in Device.devices)
                {
                    if (!d.IsFinished) return false;
                }
                return true;
            }
        }

        public bool IsTestFailed
        {
            get
            {
                foreach (Device d in Device.devices)
                {
                    if (d.IsFailed) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// device lists of int[] are concatenated ->
        /// DeviceX => int[] (index 0)
        /// DeviceY => int[] (index 1)
        /// ...
        /// </summary>
        /// <param name="concatdata"></param>
        /// <returns></returns>

        //TODO: With all this state need to reset object at some stage!!
        public DATA GetConcatData
        {
            get
            {
                if (concatdata == null)
                {
                    concatdata = new DATA();
                    foreach (Device d in Device.devices)
                    {
                        concatdata.Add(d.id, d.data);
                    }
                }
                return concatdata;
            }
        }

        public List<float[]> ChannelsSnapShot(short n_channels)
        {
            List<float[]> snapshot = new List<float[]>();
            foreach (Device d in Device.devices)
            {
                float[] aidata = new float[n_channels];
                aio.MultiAiEx(d.id, n_channels, aidata);
                snapshot.Add(aidata);
            }
            return snapshot;
        }

        public List<int[]> ChannelsSnapShotBinary(short n_channels)
        {
            List<int[]> snapshot = new List<int[]>();
            foreach (Device d in Device.devices)
            {
                int[] aidata = new int[n_channels];
                aio.MultiAi(d.id, n_channels, aidata);
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
