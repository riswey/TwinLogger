﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;



namespace MultiDeviceAIO
{
    public struct Channel
    {
        const double TOLERANCE = 0.1;
        
        //Set to refer to data source
        public static List<int[]> dataSource = null;

        public int tracknum;        //contiguous index
        public int devicenum;
        public int devicechannel;   //num per device

        public int value;
        public int zero;
        public double gain;

        private double gValue
        {
            get
            {
                return ((value - zero) / gain);
            }
        }

        public double G
        {
            get
            {
                return Math.Round(gValue, 2);
            }
        }

        public int State
        {
            get
            {
                double gVal = gValue;
                if (gVal < -1-TOLERANCE) return 0;
                if (gVal < -1+TOLERANCE) return 1;  //- ok
                if (gVal < -TOLERANCE) return 2;
                if (gVal < TOLERANCE) return 3;     //0 ok
                if (gVal < 1-TOLERANCE) return 4;
                if (gVal < 1+TOLERANCE) return 5;   //+ ok
                return 6;
            }
        }

        public Channel(int tracknum, int n_channel, int zero = 38192, double gain = 2413.5, int value = 32768)
        {
            this.tracknum = tracknum;                                               //1 based

            this.devicenum = (int)Math.Floor((double)(tracknum-1) / n_channel);     //zero based
            this.devicechannel = (tracknum-1) % n_channel;                          //zero based
            this.zero = zero;
            this.gain = gain;
            this.value = value;
        }

        public void Sync()
        {
            if (dataSource == null) return;
            if (devicenum >= dataSource.Count) return;
            if (devicechannel >= dataSource[devicenum].Length) return;

            this.value = dataSource[devicenum][devicechannel];

        }

        public void Calibrate(int zero, double gain)
        {
            this.zero = zero;
            this.gain = gain;
        }

    }

    public struct Accelerometer
    {
        //This should last the lifetime of the app!
        public static Dictionary<int, Accelerometer> accrs = new Dictionary<int, Accelerometer>();

        ///////////////////////////////////////////////////////////////////////////////////////////
        /// Static Funcs //////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////

        public static int ArrayStatus()
        {
            //No accelerometers is an error
            if (Accelerometer.accrs.Count == 0) return 2;

            //anyone out of tolerance?

            int count = 0;
            foreach (var accr in Accelerometer.accrs)
            {
                //if (!accr.Value.Status) return false;
                if (accr.Value.Status) count++;
            }
            return (count > 10)?0:1; 
            //return true;
        }

        //TODO: sort out error handling. Magic number rather than create new exception type!
        //Currently false means no file set
        //What about CSV errors!
        public static bool ImportCalibration(List<List<double>> caldata)
        {
            if (caldata == null) return false;        //none loaded yet

            foreach (KeyValuePair<int, Accelerometer> accr in Accelerometer.accrs)
            {
                accr.Value.Calibrate(caldata);
            }
            return true;
        }

        public static bool ImportMapping(List<List<int>> mapping, int n_channels)
        {
            if (mapping == null) return false;        //none loaded yet

            Accelerometer.accrs.Clear();

            foreach (List<int> row in mapping)
            {
                //NOTE: row[0] is the index for *rows* in cal file
                //row[1],[2],[3] mean orientations 0,1,2 which refer to cols in cal file
                Accelerometer.accrs.Add(row[0], new Accelerometer(row[0], n_channels, row[1], row[2], row[3]));
            }

            Accelerometer.Count = mapping.Count;
            return true;
        }

        public static void setChannelData(List<int[]> data)
        {
            //set static value faster than passing the reference loads of time
            Channel.dataSource = data;

            //tell channels to sync
            foreach (KeyValuePair<int, Accelerometer> aclr in Accelerometer.accrs)
            {
                //can't be foreach as members are const!
                for (int i = 0; i < aclr.Value.channels.Length; i++)
                {
                    aclr.Value.channels[i].Sync();
                }

            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        //each row is an accelerometer
        //col 0 = acc #
        //col 1-3 = zero
        //col 4-6 = gain
        //col 7-9 = map to channel
        //so these values are the indexes in data
        //+ >n_channels is mod, next device

        public static int Count = 0;

        public int number;
        public Channel[] channels;

        public Accelerometer(int n, int n_channels, int ch1, int ch2, int ch3)
        {
            //Default setting (Mapped but not calibrated)

            number = n;

            channels = new Channel[3];

            channels[0] = new Channel(ch1, n_channels);
            channels[1] = new Channel(ch2, n_channels);
            channels[2] = new Channel(ch3, n_channels);
        }

        //Test I am in tolerance
        public bool Status
        {
            get
            {
                //x == zero, y == 1, z == zero
                return channels[0].State == 3 && channels[1].State == 5 && channels[2].State == 3;
            }
        }

        //TODO: why can't this be protected ina struct?
        public void Calibrate(List<List<double>> caldata)
        {
            //the row number (starting 0) must match the mapping list.
            //This is not a dictionary and does not check the first column, only the row number!!!!!

            //no cal data for me
            if (this.number > caldata.Count) return;

            int calidx = this.number - 1;

            for (int orientation = 0; orientation < 3; orientation++)
            {
                int zero = (int)caldata[calidx][1 + orientation];
                double gain = caldata[calidx][4 + orientation];
                channels[orientation].Calibrate(zero, gain);
            }
        }
    }
}
