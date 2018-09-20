using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDeviceAIO
{
    public class Device
    {
        public static string DEVICENAMEROOT = "Aio00";

        public static List<Device> devices = new List<Device>();

        public short id { get; set; }
        string name;
        public List<int> data = new List<int>();
        public int target { get; set; } = 0;
        public bool IsFinished
        {
            get
            {
                return data.Count == target;
            }
        }
        public bool IsFailed { get; private set; } = false;
        //timer_duration / 1000 * sample_freq * num_channels
        //public int[] buffer = new int[1000];

        public Device(short id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public void Clear()
        {
            data.Clear();
            IsFailed = false;
        }

        public int Add(int datapointcount, ref int[] buf)
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

            if (datapointcount != 0)
            {
                var newArray = new int[datapointcount];
                Array.Copy(buf, newArray, datapointcount);
                data.AddRange(newArray);
                
            }
            else
            {
                //TODO: check logic. If gets zero data but not complete then fail it
                //Give a second chance?
                //It doesn't get a second chance if state returned to 0
                if (!IsFinished)
                {
                    IsFailed = true;
                }
            }
            
            return data.Count;
        }

        //TODO:
        //When receives zeros -> see if got enough data -> finished
        //BUT  







    }
}
