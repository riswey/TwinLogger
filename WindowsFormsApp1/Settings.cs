using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class Settings
    {
        public int nchannels { get; set; }
        public string path { get; set; }
        public bool modified { get; set; }
        public bool[] channels { get; set; }
        public int range { get; set; }
        public int duration { get; set; }
        public int parameter { get; set; }
        public int window { get; set; }
    }
}
