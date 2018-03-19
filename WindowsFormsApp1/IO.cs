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

        public static string GetFilePathCal(I_TestSettings settings, int idx)
        {
            return settings.testpath + @"\" + cal_enum[idx] + ".cal";
        }

        public static string GetFilePathTest(I_TestSettings settings)
        {
            return settings.testpath + @"\" + settings.load + (settings.clipsOn ? "A" : "B") + @"\M" + (settings.mass + 1) + ".csv";
            //return settings.frequency + "hz-M" + (settings.mass + 1) + "-" + settings.load + "kN-" + (settings.clipsOn ? "ON-" : "OFF-") + settings.n_channels + "ch-" + (settings.n_samples / settings.timer_interval) + "sec-#" + devices.Count + ".csv";
        }

        public static string GetHeader(I_TestSettings settings)
        {
            String header = settings.n_devices + ",";
            header += settings.n_channels + ",";
            header += settings.duration + ",";
            header += settings.timer_interval + ",";
            header += settings.n_samples + ",";

            header += settings.frequency + ",";
            header += (settings.mass + 1) + ",";
            header += settings.load + ",";
            header += (settings.clipsOn ? 1 : 0) + ",";
            header += settings.shakertype + ",";
            header += settings.paddtype;
            return header;
        }

        public static string getFileName(string path)
        {
            char divider = '\\';
            if (path.IndexOf('/') > 0) divider = '/';

            string[] paths = path.Split(divider);

            return paths[paths.Length - 1];
        }

        //FILE SAVING
        static public void SaveArray(I_TestSettings settings, string filepath, string header, List<List<int>> concatdata)
        {
            try
            {
                //Makes Folder if not exist
                IO.CheckPath(filepath);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath))
                {
                    file.WriteLine(header);

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

                    file.Close();
                }
            }
            catch
            {
                throw;
            }

        }

        public static int GetLine_Num(I_TestSettings settings, List<List<int>> concatdata, int line_number, ref string visitor, string delimiter = ",")
        {
            int num = 0;
            foreach (List<int> device_data in concatdata)
            {
                num += IO.GetLineId_Num(settings, device_data, line_number, ref visitor, delimiter);
            }
            return num;
        }

        public static int GetLineId_Num(I_TestSettings settings, List<int> data, int line_number, ref string visitor, string delimiter)
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
