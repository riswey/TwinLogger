using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DEVICEID = System.Int16;
//The data structure is a dictionary; K: device id V:raw list of data for device
//K: device id :. data imported by id
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;


namespace MultiDeviceAIO
{
    class IO
    {
        public static string DATAFILEFORMAT = "{TESTPATH}\\{LOAD}{CLIPSAB}\\M{MASSNUM}.jdd";

        public static string[] cal_enum = new string[] { "XP.cal", "XN.cal", "YP.cal", "YN.cal", "ZP.cal", "ZN,.cal" };

        public static string GetFilePathCal(LoggerState settings, int idx)
        {
            return settings.testpath + @"\" + cal_enum[idx] + ".cal";
        }

        public static string GetFilePathTest(LoggerState settings)
        {
            return LoggerState.MergeObjectToString(settings, DATAFILEFORMAT);
        }

        public static string GetFilePathTemp(LoggerState settings)
        {
            string timestamp = Environment.TickCount.ToString();
            string path = settings.testpath + @"\temp" + timestamp;
            settings.temp_filename = path;
            return path;
        }

        public static string getFileName(string path)
        {
            char divider = '\\';
            if (path.IndexOf('/') > 0) divider = '/';

            string[] paths = path.Split(divider);

            return paths[paths.Length - 1];
        }

        //FILE SAVING
        static public void SaveDATA(LoggerState settings, string filepath, DATA concatdata)
        {
            try
            {
                //Makes Folder if not exist
                IO.CheckPath(filepath);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath))
                {
                    int line_number = 0;
                    string str;
                    while (true)
                    {
                        str = "";
                        if (IO.GetLine_Num(settings, concatdata, line_number++, ref str, ",") == 0)
                        {
                            break;
                        }
                        file.WriteLine(str);
                    };
                    //File automatically closes with using (don't manually close!)
                }
            }
            catch
            {
                throw;
            }

        }

        public static bool MoveTempFile(PersistentLoggerState settings, string filepath)
        {
            string filename = settings.data.temp_filename;
            if (filename == null)
                return false;

            string data = File.ReadAllText(filename);
            string header = settings.GetHeader();

            CheckPath(filepath);

            File.WriteAllText(filepath, header + "\r\n" + data);
            //Clean up
            //shifted to just before get new temp file
            //File.Delete(settings.data.temp_filename);
            //settings.data.temp_filename = null;
            return true;
        }

        public static int GetLine_Num(LoggerState settings, DATA concatdata, int line_number, ref string visitor, string delimiter = ",")
        {
            int num = 0;
            foreach (KeyValuePair<DEVICEID, List<int>> device_data in concatdata)
            {
                num += IO.GetLineId_Num(settings, device_data.Value, line_number, ref visitor, delimiter);
            }
            return num;
        }

        public static int GetLineId_Num(LoggerState settings, List<int> data, int line_number, ref string visitor, string delimiter)
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
                visitor += string.Join(delimiter, str.ToArray());
                return 1;
            }

            return 0;
        }

        public static void CheckPath(string path)
        {
            FileInfo fileInfo = new FileInfo(path);

            if (!fileInfo.Exists)
                Directory.CreateDirectory(fileInfo.Directory.FullName);
        }

        static public string ReadFileHeader(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    return reader.ReadLine();
                }
            }
            return null;
        }

        /// <summary>
        /// Read file and split rows by n_channels into devices
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delimiter"></param>
        /// <param name="n_channels"></param>
        /// <param name="data"></param>
        /// <param name="header"></param>
        static public void ReadCSVConcatColumns(string filename, char delimiter, int width, out DATA data, bool header = true)
        {
            bool firstline = header;

            data = new DATA();

            using (var reader = new StreamReader(filename))
            {
                int n_devices;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (firstline)
                    {
                        //bin first line
                        firstline = false;
                    }
                    else
                    {
                        string[] values = line.Split(',');
                        List<int> ints = values.Select(int.Parse).ToList();
                        n_devices = ints.Count / width;
                        for (DEVICEID i = 0; i < n_devices; i++)
                        {
                            if (!data.ContainsKey(i))
                            {
                                data[i] = new List<int>();
                            }
                            data[i].AddRange(ints.GetRange(i * width, width));
                        }
                    }
                }
            }
        }

    }
}
