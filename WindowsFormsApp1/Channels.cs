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
using System.IO;





namespace WindowsFormsApp1
{
    public partial class Channels : Form
    {
        const int WIDTH = 25;
        const int HEIGHT = 12;
        const int OFFSET_X = 25;
        const int OFFSET_Y = 0;
        Random rnd = new Random();

        public Caio aio;
        public short id = 0;

        struct Sample
        {
            public short channels;
            public short interval;
            public short range;
            public short number;
        };

        //Max buffer = 16000 2byte ints.
        Sample s = new Sample
        {
            channels = 60,
            interval = 1000,
            range = (int)CaioConst.PM125,
            number = 1200
        };


        short AiResolution = 0;
        short nChannel = 62;
        int[] nChannelMapping = { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,-1,-1,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59};

        short[] AiChannelSeq;
        double maxbytes;

        Bitmap chVolt;
        Graphics g;

        private static readonly Dictionary<string, Pen> pens
            = new Dictionary<string, Pen>
            {
                { "Grey", new Pen(Color.Gray) },
                { "White", new Pen(Color.White) },
                { "LightGrey", new Pen(Color.LightGray) },
                { "Red", new Pen(Color.LightPink) },
                { "Amber", new Pen(Color.LightGoldenrodYellow) },
                { "Green", new Pen(Color.LightGreen) },
                { "DarkRed", new Pen(Color.DarkRed) },
                { "DarkAmber", new Pen(Color.DarkGoldenrod) },
                { "DarkGreen", new Pen(Color.DarkGreen) }
            };

        private static readonly Dictionary<string, Brush> brushes
        = new Dictionary<string, Brush>
        {
                    { "Black", new SolidBrush(Color.Black) },
                    { "White", new SolidBrush(Color.White) },
                    { "Grey", new SolidBrush(Color.Gray) },
                    { "Red", new SolidBrush(Color.Red) },
                    { "Amber", new SolidBrush(Color.Orange) },
                    { "Green", new SolidBrush(Color.Green) },
                    { "DarkRed", new SolidBrush(Color.DarkRed) },
                    { "DarkAmber", new SolidBrush(Color.DarkGoldenrod) },
                    { "DarkGreen", new SolidBrush(Color.DarkGreen) }
        };

        public Channels()
        {
            InitializeComponent();

            if (aio == null)
            {
                aio = new Caio();
                
                if (openDevice() != 0)
                {
                    toolStripStatusLabel1.Text = "Failed to Connect";
                    return;
                } else
                {
                    toolStripStatusLabel1.Text = "Device Connected";
                }

                textBox1.Text += id + "\r\n";

            }

            chVolt = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            pictureBox1.Image = chVolt;

            g = Graphics.FromImage(chVolt);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            drawGrid(g, pens["LightGrey"], OFFSET_X, OFFSET_Y, WIDTH, HEIGHT);

        }

        ~Channels()
        {
            aio.Exit(id);
            timer1.Dispose();
            g.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int ret;

/*
            //resolution (works)
            aio.GetAiResolution(id, out AiResolution);
            maxbytes = Math.Pow(2, AiResolution);

            //Doesn't work (reads 1)
            short nC1;
            aio.GetAiChannels(id, out nC1);

            //is this a mapping?
            string map = "";
            AiChannelSeq = new short[nChannel];
            for (short i = 0; i < nChannel; i++)
            {
                aio.GetAiChannelSequence(id, i, out AiChannelSeq[i]);
                map += AiChannelSeq[i].ToString() + ",";
            }
            */

            ret = aio.SetAiChannels(id, s.channels);

            ret = aio.SetAiEvent(id, (uint)this.Handle.ToInt32(), (int)(CaioConst.AIE_END | CaioConst.AIE_DATA_NUM));

            ret = aio.SetAiTransferMode(id, 0);     //Device buffered 1=sent to user memory
            ret = aio.SetAiMemoryType(id, 0);       //FIFO 1=Ring

            ret = aio.SetAiClockType(id, 0);        //internal

            ret = aio.SetAiSamplingClock(id, s.interval); //default usec (2000 for)

            ret = aio.SetAiRangeAll(id, s.range);

            ret = aio.SetAiStartTrigger(id, 0);  //0 means by software
            ret = aio.SetAiStopTrigger(id, 0);  //0 means by time

            ret = aio.SetAiStopTimes(id, s.number);     //5000 sampling at 1000usec

            ret = aio.ResetAiMemory(id);

            textBox1.Text += "START" + "\r\n";

            ret = aio.StartAi(id);

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //all return ret == 0/7(fromstandby) is ok > 10000 prob


            g.Clear(Color.Transparent);

            drawGrid(g, pens["LightGrey"], OFFSET_X,OFFSET_Y,WIDTH,HEIGHT);

            //get channel list
            int[] aidata = new int[nChannel];
            long ret = aio.MultiAi(id, nChannel, aidata);

            String str = "Channel Voltages" + "\r\n" + "================\r\n";
            for (int ch = 0; ch < aidata.Length; ch++)
            {

                if (nChannelMapping[ch] < 0) continue;


                //System Channel is actually channel map cm
                int cm = nChannelMapping[ch];

                double volt = Math.Round((aidata[ch] - maxbytes / 2) / maxbytes * 20, 3);
                volt = 1.6;

                if (volt < 1.35) volt = 1.35;
                if (volt > 2.05) volt = 2.05;
                int normvolt = (int)Math.Round((volt - 1.35) / 2.05);

                if (cm % 3 == 0)
                {
                    str += "\r\n" + ch + ":" + "\t";
                }

                str += volt.ToString() + "\t";


                drawMeter(g, cm % 3, (int)Math.Floor((double)cm / 3), 50, 20, volt, 1.35, 2.05);
                /*
                if (volt > 1.79)
                {
                    g.FillEllipse(rdbsh,
                        25 + (cm % 3) * 50 - rad / 2,
                        20 + (int)Math.Floor((double)cm / 3) * 17 - rad / 2,
                        rad,
                        rad
                    );
                } else
                {
                    g.FillEllipse(gnbsh,
                        25 + (cm % 3) * 50 - rad / 2,
                        20 + (int)Math.Floor((double)cm / 3) * 17 - rad / 2,
                        rad,
                        rad
                    );

                }

                g.DrawEllipse(gypen,
                    20 + (ch % 3) * 50,
                    15 + (int)Math.Floor((double)cm / 3) * 17,
                    11,
                    11
                );
                */
            }

            this.textBox1.Text = str;
            pictureBox1.Image = chVolt;
            

        }

        void drawMeter(Graphics g, int cell_x, int cell_y, int width, int height, double volt, double volt_min, double volt_max)
        {
            Pen pen = pens["LightGrey"];
            Brush brush1 = brushes["White"];
            Brush brush2 = brushes["White"];
            Brush brush3 = brushes["White"];
            
            double normvolt = rnd.NextDouble(); //(volt - volt_min) / volt_max;

            if (normvolt < 0.33)
            {
                brush1 = brushes["Green"];
            }
            else if (normvolt > 0.67)
            {
                brush3 = brushes["Red"];
            }
            else
            {
                brush2 = brushes["Amber"];
            }

            int rad = 12;
            int x = OFFSET_X + cell_x * width;
            int y = OFFSET_Y + cell_y * height;

            g.FillEllipse(brush1, x, y+HEIGHT/2, rad, rad);
            g.FillEllipse(brush2, x+rad+1, y + HEIGHT / 2, rad, rad);
            g.FillEllipse(brush3, x+2*rad + 2, y + HEIGHT / 2, rad, rad);
            Font f = new Font(FontFamily.GenericMonospace, 10);
            //g.DrawString( Math.Round(normvolt,2).ToString() , f, brushes["Black"], x, y+10);
            
        }

        private void drawGrid(Graphics g, Pen p, int offset_x, int offset_y, int width, int height)
        {

            float y;
            for (int i = 0; i < 21; i++)
            {
                y = i * height + offset_y;
                g.DrawLine(p, 0, y, 180, y);
                //g.DrawString(i,)
            }
            //g.DrawLine(gypen, 0, y, 180, y);
            //g.DrawLine(gypen, 0, y, 180, y);

        }


        long openDevice()
        {
            string device_root = "Aio00";
            
            long ret = 1;
            string device_name;
            for(int i=0;i<4;i++)
            {
                device_name = device_root + i;
                ret = aio.Init(device_name, out id);
                if (ret == 0)
                {
                    //Success
                    break;
                }
            }

            return ret;
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x1002:
                    {
                        textBox1.Text += "End" + m.LParam;
                        textBox1.Invalidate();

                        int AiSamplingTimes = s.number;
                        int[] AiData = new int[s.channels * s.number];
                        int ret = aio.GetAiSamplingData(id, ref AiSamplingTimes, ref AiData);

                        string header = "Channels: " + s.channels + ", Interval: " + s.interval + "us, Samples: " + s.number;
                        string path = "demoout.csv";
                        string str;
                        try
                        {
                            using (System.IO.StreamWriter file =
                                new System.IO.StreamWriter(path))
                            {
                                file.WriteLine(header);

                                for (int n = 0; n < s.number; n++)
                                {
                                    str = AiData[n].ToString();
                                    for (int c = 1; c < s.channels; c++)
                                    {
                                        str += "," + AiData[c * s.number + n].ToString();
                                    }
                                    file.WriteLine(str);
                                }
                                file.Close();
                                toolStripStatusLabel1.Text = "Data saved";
                            }
                        } catch
                        {
                            toolStripStatusLabel1.Text = "Data not saved";
                        }
                        textBox1.Text = "Finished";

                        textBox1.Invalidate();
                    }                    
                    break;
                case 0x1003:
                    {
                        textBox1.Text += "NUM " + m.LParam;
                        textBox1.Invalidate();
                    }
                    break;
            }

            base.WndProc(ref m);
        }
    }
}
