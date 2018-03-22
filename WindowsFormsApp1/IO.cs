using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MultiDeviceAIO
{
    class IO
    {
        public static string[] cal_enum = new string[] { "XP.cal", "XN.cal", "YP.cal", "YN.cal", "ZP.cal", "ZN,.cal" };

        public static string GetFilePathCal(SettingData settings, int idx)
        {
            return settings.testpath + @"\" + cal_enum[idx] + ".cal";
        }

        public static string GetFilePathTest(SettingData settings)
        {
            return settings.testpath + @"\" + settings.load + (settings.clipsOn ? "A" : "B") + @"\M" + (settings.mass + 1) + ".csv";
            //return settings.frequency + "hz-M" + (settings.mass + 1) + "-" + settings.load + "kN-" + (settings.clipsOn ? "ON-" : "OFF-") + settings.n_channels + "ch-" + (settings.n_samples / settings.timer_interval) + "sec-#" + devices.Count + ".csv";
        }

        public static string GetFilePathTemp(SettingData settings)
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
        static public void SaveArray(SettingData settings, string filepath, List<List<int>> concatdata)
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

        public static bool MoveTempFile(AIOSettings settings, string filepath)
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

        public static int GetLine_Num(SettingData settings, List<List<int>> concatdata, int line_number, ref string visitor, string delimiter = ",")
        {
            int num = 0;
            foreach (List<int> device_data in concatdata)
            {
                num += IO.GetLineId_Num(settings, device_data, line_number, ref visitor, delimiter);
            }
            return num;
        }

        public static int GetLineId_Num(SettingData settings, List<int> data, int line_number, ref string visitor, string delimiter)
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
                visitor += string.Join(delimiter, str.ToArray() );
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
    }


}
