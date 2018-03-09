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
    class MyMultiAIO : I_MyAIO
    {
        //static string device_root = "Aio00";

        List<MyAIO> devices = new List<MyAIO>();

        int data_ready = 0;

        public void Init(string device_name)        //add devices
        {
            Caio aio = new Caio();

            long ret = 1;
            short id1 = 0;
            string full_device_name;
            for (int i = 0; i < 10; i++)
            {
                full_device_name = device_name + i;
                ret = aio.Init(full_device_name, out id1);
                if (ret == 0)
                {
                    MyAIO myaio = new MyAIO();
                    myaio.Init(full_device_name);
                    devices.Add( myaio );
                }
            }
            aio = null;
        }

        ~MyMultiAIO()
        {
            Close();
        }

        public void Close()
        {
            foreach (MyAIO myaio in devices)
            {
                myaio.Close();
            }

            devices.Clear();
        }

        public void Reset()
        {
            data_ready = 0;
            foreach (MyAIO myaio in devices)
            {
                myaio.Reset();
            }
        }

        private MyAIO findDevice(int id)
        {
            foreach(MyAIO myaio in devices)
            {
                if (myaio.id == id)
                {
                    return myaio;
                }
            }
            return null;
        }

        public void SetupExternalParameters(double frequency, bool clipsOn)
        {
            foreach (MyAIO myaio in devices)
            {
                myaio.SetupExternalParameters(frequency, clipsOn);
            }

        }

        public int SetupTimedSample(short n_channels, short timer_interval, short n_samples, CaioConst range)
        {
            int ret = 0;
            foreach (MyAIO myaio in devices)
            {
                ret += myaio.SetupTimedSample(n_channels, timer_interval, n_samples, range);
            }
            return ret;
        }

        public int Start(uint HandleMsgLoop)
        {
            int ret = 0;
            foreach (MyAIO myaio in devices)
            {
                ret += myaio.Start(HandleMsgLoop);
            }
            return ret;
        }

        public void Stop()
        {
            foreach (MyAIO myaio in devices)
            {
                myaio.Stop();
            }
        }

        public void PrepareData(int device_id, int num_samples)
        {
            try
            {
                findDevice(device_id).PrepareData(device_id, num_samples);
                data_ready++;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string GetHeader(string delimiter)
        {
            return "";
        }

        public void Print(string delimiter, ref string visitor)
        {
            List<string> str = new List<string>();
            foreach (MyAIO myaio in devices)
            {
                myaio.Print(delimiter, ref visitor);
            }
        }

        public string SaveData()
        {
            if (data_ready != devices.Count)
            {
                //still waiting for devices to return
                return null;
            }

            //All the devices have data ready. Save the whole data set.

            string header = "";// "Device," + device_number + "\nChannels," + n_channels + "\nInterval (us)," + timer_interval + "\nSamples," + n_samples;

            string path = "";
            //long time = DateTimeOffset.Now.ToUnixTimeSeconds();
            string filename = "";// this.frequency + "hz-" + (this.clipsOn ? "ON-" : "OFF-") + n_channels + "ch-" + (n_samples / timer_interval) + "sec-#" + this.device_number + ".csv";
            try
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(filename))
                {
                    file.WriteLine(header);
                    string str;
                    while (true)
                    {
                        str = "";
                        Print(",", ref str);
                        if (str == null) break;
                        file.WriteLine(str);
                    };

                    file.Close();
                    return path + filename;
                }
            }
            catch
            {
                throw;
            }
        }

    }
}
