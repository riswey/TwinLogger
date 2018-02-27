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
    public partial class Monitor : Form
    {
        public string device_name;

        Caio aio = new Caio();
        short id;

        const int WIDTH = 25;
        const int HEIGHT = 12;
        const int OFFSET_X = 25;
        const int OFFSET_Y = 0;
        Random rnd = new Random();

        short nChannel = 62;
        int[] nChannelMapping = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, -1, -1, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59 };
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


        public Monitor()
        {
            InitializeComponent();

            int ret = aio.Init(device_name, out id);

            chVolt = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            pictureBox1.Image = chVolt;

            g = Graphics.FromImage(chVolt);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            drawGrid(g, pens["LightGrey"], OFFSET_X, OFFSET_Y, WIDTH, HEIGHT);

            timer1.Start();

        }

        ~Monitor()
        {
            timer1.Dispose();
            g.Dispose();
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

            g.FillEllipse(brush1, x, y + HEIGHT / 2, rad, rad);
            g.FillEllipse(brush2, x + rad + 1, y + HEIGHT / 2, rad, rad);
            g.FillEllipse(brush3, x + 2 * rad + 2, y + HEIGHT / 2, rad, rad);
            Font f = new Font(FontFamily.GenericMonospace, 10);
            //g.DrawString( Math.Round(normvolt,2).ToString() , f, brushes["Black"], x, y+10);
        }

        private void drawGrid(Graphics g, Pen p, int offset_x, int offset_y, int width, int height)
        {
            /*
            float y;
            for (int i = 0; i < 21; i++)
            {
                y = i * height + offset_y;
                g.DrawLine(p, 0, y, 180, y);
                //g.DrawString(i,)
            }
            //g.DrawLine(gypen, 0, y, 180, y);
            //g.DrawLine(gypen, 0, y, 180, y);
            */
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            //all return ret == 0/7(fromstandby) is ok > 10000 prob
            g.Clear(Color.Transparent);

            drawGrid(g, pens["LightGrey"], OFFSET_X, OFFSET_Y, WIDTH, HEIGHT);

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

            //this.textBox1.Text = str;
            pictureBox1.Image = chVolt;
        }
    }
}
