using System;
using System.Collections.Generic;
using System.Linq;
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

    public class MyAIO
    {
        //TODO: this will crash if not installed. Check
        public Caio aio;

        public static readonly Dictionary<CaioConst, string> AIOSTATUS = new Dictionary<CaioConst, string>() {
            {0, "Ready" },
            {CaioConst.AIS_BUSY, "Device is running"},
            {CaioConst.AIS_START_TRG, "Wait the start trigger"},
            {CaioConst.AIS_DATA_NUM, "Store up to the specified number of data"},
            {CaioConst.AIS_OFERR, "Overflow"},
            {CaioConst.AIS_SCERR, "Sampling clock error"},
            {CaioConst.AIS_AIERR, "AD conversion error"},
            {CaioConst.AIS_DRVERR, "Driver spec error"}
        };

        public static bool TestBit(object code, object bit)
        {
            if ((int)code == 0 && (int)bit == 0)
            {
                return true;
            }

            return ((int)code & (int)bit) != 0;
        }


        public enum DEVICESTATE { READY, ARMED, SAMPLING }
        public enum DEVICESTATEDELTA { NONE, READY, ARMED, SAMPLING }       //measures boundary events

        private static DEVICESTATE devicestate = DEVICESTATE.READY;

        /*
         * Delta measures the first device to change state.
         * TODO: maybe make it measure the last
         */

        //TODO: Actually device state is something that devices can manage!!!!!
        //TODO: This will also handle the delta on first || last device event problem 
        public void SampleDeviceState(ref List<int> status, out int bitflags, out DEVICESTATEDELTA delta)
        {
            //set status list
            GetStatusAll(ref status);

            //Prepare bit flags
            bitflags = 0;
            foreach (int s1 in status)
            {
                bitflags |= s1;
            }

            //Respond to bigflags
            //NOTE: 1 == AIS_BUSY (TODO: any use?)
            delta = DEVICESTATEDELTA.NONE;

            if (TestBit(bitflags, 0))       //Idle
            {
                if (devicestate == DEVICESTATE.SAMPLING)
                    delta = DEVICESTATEDELTA.READY;

                devicestate = DEVICESTATE.READY;
            }

            //Waiting for trigger
            if (TestBit(bitflags, CaioConst.AIS_START_TRG))
            {
                if (devicestate == DEVICESTATE.READY)
                    delta = DEVICESTATEDELTA.ARMED;

                devicestate = DEVICESTATE.ARMED;
            }

            //Started sampling
            if (TestBit(bitflags, CaioConst.AIS_DATA_NUM))
            {
                if (devicestate == DEVICESTATE.ARMED || devicestate == DEVICESTATE.READY)
                    delta = DEVICESTATEDELTA.SAMPLING;

                devicestate = DEVICESTATE.SAMPLING;
            }

        }

        public double testtarget { get; private set; } = 1e5;

        //Ensure you have only one copy of this!
        public DATA concatdata { get; private set; } = null;      //Keep for Scope


        public MyAIO(int testing)
        {
            if (testing != 0)
                aio = new CaioTest();
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
                switch (value)
                {
                    case 0:
                        return;
                    case 7:
                        //You got a code 7!
                        throw new Exception("Got a 7 from Device (wake from idle)");
                        //TODO ResetDevices();
                        return;
                    case CaioTest.NOTIMPLEMENTED:
                        throw new NotImplementedException("CaioTest method");
                    default:
                        aio.GetErrorString((int)value, out string ErrorString);
                        throw new Exception(ErrorString);
                }

            }
        }

        public int DiscoverDevices()        //add Device.devices
        {
            //TODO: rather than looking at error code is there a proactive way to find devices? 
            DEVICEID id = 0;
            string name;

            Device.devices.Clear();

            for (int i = 0; i < 10; i++)
            {
                name = Device.DEVICENAMEROOT + i;
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
                //TODO: this causing problems if the device is frozen
                //It failed in USB transfer
                HANDLE_RETURN_VALUES = aio.StopAi(d.id);
                HANDLE_RETURN_VALUES = aio.ResetAiMemory(d.id);
            }
        }

        public bool DeviceCheck()
        {
            return true;
            //this happens each click now!
            /*
            foreach(Device d in Device.devices)
            {
                if (GetStatus(d.id) == null)
                {
                    return false;
                }
            }
            return true;
            */
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

        /*
         * This will hang if the device has failed
         * 
         * 
         * 
         */

        public void ResetDevices()
        {
            //Resets Device.devices and Drivers
            foreach (Device d in Device.devices)
            {
                try
                {
                    HANDLE_RETURN_VALUES = aio.ResetDevice(d.id);
                }
                catch (Exception ex)
                {
                    //It failed in USB transfer.
                    //TODO low level USB check
                }
            }

            DiscoverDevices();

            //TODO: how programatically disconnect and reconnect dvices
            //They show in device manager but not to program
            //How to debug usb devices? i.e. they show but why not showing up in prog
        }
        /*
        private int GetStatus(DEVICEID id)
        {
            HANDLE_RETURN_VALUES = aio.GetAiStatus(id, out int AiStatus);
            /*
            if (AIOSTATUS.ContainsKey((CaioConst)AiStatus))
            {
                AIOSTATUS.TryGetValue((CaioConst)AiStatus, out string status);
                return status;
            }
            */
            //return AiStatus;
        //}
        

        public void GetStatusAll(ref List<int> status)
        {
            /*  //Working State
                AIS_BUSY		Device is running
                AIS_START_TRG	Wait the start trigger
                AIS_DATA_NUM	Store up to the specified number of data
                //Error State
                AIS_OFERR		Overflow
                AIS_SCERR		Sampling clock error
                AIS_AIERR		AD conversion error
                AIS_DRVERR		Driver spec error
             */

            status.Clear();
            foreach (Device d in Device.devices)
            {
                HANDLE_RETURN_VALUES = aio.GetAiStatus(d.id, out int AiStatus);
                status.Add(AiStatus);           
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

            testtarget = 0;

            //Setting the 
            foreach (Device d in Device.devices)
            {
                HANDLE_RETURN_VALUES = aio.SetAiChannels(d.id, settings.n_channels);
                HANDLE_RETURN_VALUES = aio.SetAiSamplingClock(d.id, settings.timer_interval);  //default usec (2000 for)
                HANDLE_RETURN_VALUES = aio.SetAiStopTimes(d.id, settings.n_samples);

                HANDLE_RETURN_VALUES = aio.SetAiMemoryType(d.id, 0);                      //FIFO 1=Ring

                HANDLE_RETURN_VALUES = aio.SetAiStopTrigger(d.id, 0);                     //0 means by time

                if (settings.external_trigger)
                {
                    HANDLE_RETURN_VALUES = aio.SetAiStartTrigger(d.id, 1);                //1 by External trigger rising edge
                }
                else
                {
                    HANDLE_RETURN_VALUES = aio.SetAiStartTrigger(d.id, 0);                //0 by Software
                }

                if (settings.external_clock)
                {
                    HANDLE_RETURN_VALUES = aio.SetAiClockType(d.id, 1);                   //external
                }
                else
                {
                    HANDLE_RETURN_VALUES = aio.SetAiClockType(d.id, 0);                   //internal
                }

                //This is the expected number of samples per device
                testtarget += d.target = settings.sample_frequency * settings.duration * settings.n_channels;

                //timer_duration / 1000 * sample_freq * num_channels *2 (no chance of overflow!)
                //d.buffer = new int[TIMERPERIOD / 1000 * settings.sample_frequency * settings.n_channels * 2];

            }

            //Reset data state
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

        /*
         *   Returns total size of combined device buffers so far
         */
        public int RetrieveAllData()
        {
            int t = 0;
            //Added ToList() :. single random collection mod error 
            foreach (Device d in Device.devices.ToList())
            {
                t += RetrieveData(d);
            }
            return t;
        }

        /*
         * NOTE:
         * This will throw exceptions arising from accessing the device buffer
         * 1664LAX does not support user defined buffer. We are stuck with their flakey one.
         * 
         * Won't bubble exceptions into next layer. Using magic number
         * 
         */
        private int RetrieveData(Device d)
        {
            HANDLE_RETURN_VALUES = aio.GetAiSamplingCount(d.id, out int sampling_count);

            int data_points = sampling_count * PersistentLoggerState.ps.data.n_channels;

            //10% too large cos sampling_count not guaranteed
            int[] buffer = GetBuffer(data_points);

            if (data_points > 0)
            {
                //NOTE: warning this is where memory problems may occur
                HANDLE_RETURN_VALUES = aio.GetAiSamplingData(d.id, ref sampling_count, ref buffer);
                return d.Add(data_points, ref buffer);
            }

            return 0;
        }

        private int[] _buffer = new int[1000];
        private ref int[] GetBuffer(int size)
        {
            //Allow 10% error
            if (size * 1.1 > _buffer.Length)
            {
                _buffer = new int[(int)(size * 1.1)];
            }
            return ref _buffer;
        }

        public bool IsTestFinished {
            get
            {
                Debug.WriteLine("Check Fnshed: #" + Device.devices.Count);

                foreach (Device d in Device.devices)
                {
                    Debug.WriteLine("Check if Finished...");

                    if (!d.IsFinished) return false;
                }
                Debug.WriteLine("Finished==true");

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

                    Device.devices.ForEach(d => concatdata.Add(d.id, d.data));
                    /*
                    foreach (Device d in Device.devices)
                    {
                        concatdata.Add(d.id, d.data);
                    }
                    */
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



        /***********************************
         *
         *    CALLBACK STUFF FOR START
         *
         **********************************/
        /*
       //copied from CaioCS
       unsafe public delegate int PAICALLBACK(short Id, short Message, int wParam, int lParam, void* Param);

       //Note that in loop version this was done in start
       unsafe public void SetAiCallBackProc(IntPtr pAiCallBack)
       {
           // Set the callback routine : Device Operation End Event Factor
           foreach (Device d in Device.devices)
           {
               HANDLE_RETURN_VALUES = aio.SetAiCallBackProc(d.id, pAiCallBack, (int)(CaioConst.AIE_START), null);
           }
       }
       */

        #region TESTING

        public void SimulateTrigger()
        {
            /* In real environment the Motor Control will Send a SS signal to Ardunio.
             * -> Arduino will trigger LAX1664
             * -> LAX1664 will enter DATA_NUM
             * -> Poller will respond to LAX1664
             * 
             * In Test environment can be called by:
             * Start Button (internal trigger effectively)
             * Simulated on a timer to trigger "sometime later" 
             * By MotorControl (Send SS includes this call)
             *
             * PS: this is here so as to keep the API the same in Caio modules
             */


            //Check that its the test environment
            if (aio.GetType() == typeof(CaioTest))
            {
                //Do a Test Trigger of the LAX1664
                ((CaioTest)aio).devicestate = ((CaioTest)aio).devicestate.ToDictionary(p => p.Key, p => CaioConst.AIS_DATA_NUM);

            }
        }


        #endregion



    }

}
