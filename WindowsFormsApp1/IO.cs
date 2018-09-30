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
    public class IO
    {
        public static string[] cal_enum = new string[] { "XP.cal", "XN.cal", "YP.cal", "YN.cal", "ZP.cal", "ZN,.cal" };

        public static string GetFilePathCal(LoggerState settings, int idx)
        {
            return settings.testpath + @"\" + cal_enum[idx] + ".cal";
        }

        public static string GetFilePathTest(LoggerState settings, string datafileformat, string extension)
        {
            //TODO: chekc its a valid filename
            return LoggerState.MergeObjectToString<TestPropertyAttribute>(settings, datafileformat) + "." + extension;
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
        static public void SaveDATA(LoggerState settings, ref string filepath, DATA concatdata)
        {
            //try
            {
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
            //catch
            {
            //    throw;
            }

        }

        public static bool MoveTempFileAddHeader(PersistentLoggerState settings, string filepath)
        {
            string filename = settings.data.temp_filename;
            if (filename == "")
                return false;

            string data = File.ReadAllText(filename);
            string header = settings.GetHeader();

            filepath = PreparePath(filepath);

            File.WriteAllText(filepath, header + "\r\n" + data);
            //Clean up
            File.Delete(settings.data.temp_filename);
            settings.data.temp_filename = "";
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

        public static bool DirExists(string path)
        {
            //keep IO stuff here + ensure this works
            return Directory.Exists(path);
        }

        public static string PreparePath(string path, bool overwrite = false)
        {
            FileInfo fileInfo = new FileInfo(path);
            //if not exists will be created
            Directory.CreateDirectory(fileInfo.Directory.FullName);

            if (!overwrite)
            {
                while (File.Exists(path))
                {
                    path = transformToFreeFilename(path);
                }
            }

            return path;
        }

        public static string transformToFreeFilename(string filepath)
        {
            string fn = Path.GetDirectoryName(filepath) + "\\" + Path.GetFileNameWithoutExtension(filepath);
            string ext = Path.GetExtension(filepath);
            int pos = fn.LastIndexOf('_', fn.Length - 2); //eliminates fn ending in "_"
            string root = fn.Substring(0, pos);
            string end = fn.Substring(pos + 1); //safe as pos != length
            if (pos >= 0)
            {
                if (int.TryParse(end, out int n))
                {
                    //increment index
                    return root + "_" + (n + 1) + ext;
                }
                //end is not actually a number. Reassemble and + rename index
                return root + "_" + end + "_" + (n + 1) + ext;

            }
            //just _ rename index
            return root + "_1" + ext;
        }

        static public string ReadFileHeader(string filename, char delimiter = ',')
        {
            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string header = reader.ReadLine();
                }
            }
            return null;
        }

        /* deprecated
        /// <summary>
        /// Read file and split rows by n_channels into devices
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delimiter"></param>
        /// <param name="n_channels"></param>
        /// <param name="data"></param>
        /// <param name="header"></param>
        static public void ReadCSVConcatColumns(string filename, char delimiter, int width, out DATA data, bool hasheader = true)
        {
            bool firstline = hasheader;

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
        */

        //IO.ReadCSV<int>(filename, IO.DelegateParseInt<int>, out List<List<int>> dataall,',',true);
        static public void ConvertJJD2DATA(List<List<int>> array, int n_channels, out DATA data)
        {
            //jjd file
            data = new DATA();

            int n_devices = array[0].Count / n_channels;

            foreach (List<int> row in array) {
                for (DEVICEID i = 0; i < n_devices; i++)
                {
                    if (!data.ContainsKey(i))
                    {
                        data[i] = new List<int>();
                    }
                    data[i].AddRange(row.GetRange(i * n_channels, n_channels));
                }
            }
        }

        public delegate List<T> ProcessRow<T>(string[] row);

        //conforms to ProcessRow
        static public List<double> DelegateParseDouble<T>(string[] row)
        {
            List<double> list = row.Select(double.Parse).ToList();
            return list;
        }

        //conforms to ProcessRow
        static public List<int> DelegateParseInt<T>(string[] row)
        {
            List<int> list = row.Select(int.Parse).ToList();
            return list;
        }

        ///<exception cref="System.IO.FileNotFoundException">Check filename exists</exception>
        static public void ReadCSV<T>(string filename, ProcessRow<T> processrow, out List<List<T>> data, char delimiter = ',', bool hasheader = false )
        {
            bool firstline = hasheader;

            data = new List<List<T>>();

            using (var reader = new StreamReader(filename))
            {
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
                        string[] values = line.Split(delimiter);
                        List<T> row = processrow(values);
                        data.Add(row);
                    }
                }
            }
        }

    }
}
