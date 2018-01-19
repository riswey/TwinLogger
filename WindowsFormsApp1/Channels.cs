using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaioCs;


namespace WindowsFormsApp1
{
    public partial class Channels : Form
    {
        public Caio aio;
        public short id = 0;

        public Channels()
        {
            InitializeComponent();
        }

        ~Channels()
        {
            timer1.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //resolution
            short AiResolution;
            aio.GetAiResolution(id, out AiResolution);

            //int maxbytes = Math.Pow(2.0, (douAiResolution);

            short AiChannels = 60;
            //aio.GetAiChannels(id, out AiChannels);

            int[] aidata = new int[AiChannels];
            long ret = aio.MultiAi(id, AiChannels, aidata);

            String str = "Channel Voltages" + "\r\n" + "================\r\n";
            for (int ch = 0; ch < aidata.Length; ch++)
            {

                double volt = Math.Round((aidata[ch] - 65536.0 / 2) / 65536.0 * 20, 3);

                if (ch % 3 == 0)
                {
                    str += "\r\n" + ch + ":" + "\t";
                }

                str += volt.ToString() + "\t";

            }

            this.textBox1.Text = str;

        }
    }
}
