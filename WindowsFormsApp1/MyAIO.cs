using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaioCs;

/*
 *  devices need to be reset
 * 
 * assume all devices data gets prepared.
 * 
 * when last id gets called call the save data
 * 
 */


namespace MultiDeviceAIO
{
    using DEVICEID = System.Int16;

    public class MyAIO
    {

        //static string device_root = "Aio00";
        const string LINEEND = "\r\n";

        //TODO: this will crash if not installed. Check
        Caio aio = new Caio();

        I_TestSettings settings;

        //public Settings settings = new Settings();

        List<DEVICEID> devices = new List<DEVICEID>();
        public Dictionary<DEVICEID, string> devicenames = new Dictionary<DEVICEID, string>();
        public Dictionary<DEVICEID, List<int[]>> data = new Dictionary<DEVICEID, List<int[]> >();

        private int finished_count = 0;

        //TODO: test whether this object reflects the object in ProcessSettings!!!!
        public MyAIO(I_TestSettings settings)
        {
            this.settings = settings;
        }

        ~MyAIO()
        {
            Close();
        }

        public void DiscoverDevices(string device_root)        //add devices
        {
            long ret = 1;
            DEVICEID id = 0;
            string full_device_name;
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
            data.Clear();
            foreach(DEVICEID id in devices)
            {
                data[id] = new List<int[]>();
            }

            foreach (DEVICEID id in devices)
            {
                aio.ResetAiMemory(id);
            }
        }

        public int SetupTimedSample(short n_channels, short timer_interval, short n_samples, CaioConst range)
        {
            int ret = 0;
            foreach (DEVICEID id in devices)
            {
                settings.n_channels = n_channels;
                settings.timer_interval = timer_interval;
                settings.n_samples = n_samples;

                ret = aio.SetAiChannels(id, settings.n_channels);
                ret += aio.SetAiTransferMode(id, 0);                //Device buffered 1=sent to user memory
                ret += aio.SetAiMemoryType(id, 0);                  //FIFO 1=Ring
                ret += aio.SetAiClockType(id, 0);                   //internal
                ret += aio.SetAiSamplingClock(id, settings.timer_interval);  //default usec (2000 for)
                ret += aio.SetAiStartTrigger(id, 0);                //0 means by software
                ret += aio.SetAiStopTrigger(id, 0);                 //0 means by time
                ret += aio.SetAiStopTimes(id, settings.n_samples);
                ret += aio.ResetAiMemory(id);
                ret += aio.SetAiEventSamplingTimes(id, 100);        //#samples until asks data retrieve

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

        public void Stop()
        {
            foreach (DEVICEID id in devices)
            {
                aio.StopAi(id);
            }

            Reset();
        }

        public int RetrieveData(DEVICEID device_id, int num_samples)
        {
            int sampling_times = num_samples;
            int[] data1 = new int[settings.n_channels * num_samples];

            int ret = aio.GetAiSamplingData(device_id, ref sampling_times, ref data1);

            //if sampling times changes then sampling cut short

            if (ret == 0) {
                //store data
                data[device_id].Add( data1 );   
            }
            return ret;
        }

        private int GetLine_Num(Dictionary<DEVICEID, List<int>> data, int line_number, ref string visitor, string delimiter = ",")
        {
            int num = 0;
            foreach (DEVICEID id in devices)
            {
                num += GetLineId_Num(data[id], line_number, ref visitor, delimiter);
            }
            return num;
        }

        private int GetLineId_Num(List<int> data, int line_number, ref string visitor, string delimiter)
        {
            if (line_number < settings.n_samples)
            {
                int start = line_number * settings.n_channels;
                int end = start + settings.n_channels;

                //Separate new data from existing data
                if (visitor.Length != 0)
                {
                    visitor += delimiter;
                }

                //Add to existing
                List<string> str = new List<string>();
                for (int i = start; i < end; i++)
                {
                    str.Add(data[i].ToString());
                }
                visitor += string.Join(delimiter, str);
                return 1;
            }

            return 0;
        }

        public void DeviceFinished()
        {
            finished_count++;
        }

        public bool TestFinished()
        {
            return finished_count == devices.Count;
        }

        private string GetFilename()
        {
            return settings.frequency + "hz-M" + (settings.mass+1) + "-" + settings.load + "kN-" + (settings.clipsOn ? "ON-" : "OFF-") + settings.n_channels + "ch-" + (settings.n_samples / settings.timer_interval) + "sec-#" + devices.Count + ".csv";
        }

        private string GetHeader()
        {
            string header = settings.n_samples + ",";
            header += devices.Count + ",";
            header += settings.frequency + ",";
            header += (settings.mass + 1) +",";
            header += settings.load + ",";
            header += (settings.clipsOn ? 1 : 0) + ",";
            header += settings.n_channels;
            return header;
        }

        public string SaveData()
        {
            if (!TestFinished()) return null;

            //Concat data
            Dictionary<DEVICEID, List<int>> data_concat = new Dictionary<DEVICEID, List<int>>();
            foreach(DEVICEID id in devices)
            {
                data_concat[id] = new List<int>();
                foreach (int[] bundle in data[id])
                {
                    data_concat[id].AddRange(bundle);
                }
            }

            string path = settings.testpath + "\\";
            //long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string filename = path + GetFilename();
            try
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(filename))
                {
                    int line_number = 0;
                    string str = "";

                    file.WriteLine( GetHeader() );

                    while(true)
                    {
                        str = "";
                        if (this.GetLine_Num(data_concat, line_number++, ref str, ",") == 0)
                        {
                            break;
                        }
                        file.WriteLine(str);
                    };

                    file.Close();
                    return filename;
                }
            }
            catch
            {
                throw;
            }
        }

        public List<float[]> ChannelsSnapShot()
        {
            List<float[]> snapshot = new List<float[]>();
            foreach (DEVICEID id in devices)
            {
                float[] aidata = new float[settings.n_channels];
                long ret = aio.MultiAiEx(devices[id], settings.n_channels, aidata);
                snapshot.Add(aidata);
            }
            return snapshot;
        }

    }
}
