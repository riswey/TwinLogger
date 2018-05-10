using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CaioCs;

using System.Diagnostics;

namespace MultiDeviceAIO
{
    public partial class Monitor : Form
    {
        public MyAIO aio;

        short n_channels;

        float volt_min = 10f;
        float volt_max = -10f;

        const int WIDTH = 25;
        const int HEIGHT = 12;
        const int OFFSET_X = 25;
        const int OFFSET_Y = 0;
        const int PANEL_OFFSET = 350;

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
                { "Transparent", new SolidBrush(Color.Transparent) },
                { "Grey", new SolidBrush(Color.Gray) },
                { "Red", new SolidBrush(Color.Red) },
                { "Amber", new SolidBrush(Color.Orange) },
                { "Green", new SolidBrush(Color.Green) },
                { "DarkRed", new SolidBrush(Color.DarkRed) },
                { "DarkAmber", new SolidBrush(Color.DarkGoldenrod) },
                { "DarkGreen", new SolidBrush(Color.DarkGreen) }
        };

        static Font f = new Font(FontFamily.GenericMonospace, 10);

        public Monitor(MyAIO aio, short n_channels)
        {
            InitializeComponent();

            this.aio = aio;
            this.n_channels = n_channels;

            chVolt = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            pictureBox1.Image = chVolt;

            g = Graphics.FromImage(chVolt);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            timer1.Start();

        }

        ~Monitor()
        {
            timer1.Dispose();
            g.Dispose();
        }

        protected override void OnShown(EventArgs e)
        {
            //Caller has set the aio object now
            base.OnShown(e);
            if (aio.GetID(0) != -1) label1.Text = "Device: " + aio.devicenames[aio.GetID(0)];
            if (aio.GetID(1) != -1) label2.Text = "Device: " + aio.devicenames[aio.GetID(1)];
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            g.Clear(Color.Transparent);

            DrawGrid(g);

            List<float[]> snapshot = aio.ChannelsSnapShot(n_channels);

            if (aio.devicenames.Count > 0) DrawDeviceChannels(0, snapshot[0]);
            if (aio.devicenames.Count > 1) DrawDeviceChannels(1, snapshot[1]);

            pictureBox1.Image = chVolt;
        }

        void DrawDeviceChannels(int device, float[] data)
        {
            int cm = 0;
            float volt;
            for (int ch = 0; ch < data.Length - 2; ch++)
            {
                if (ch == 30 || ch == 31) continue;

                //if (device == 1 && ch >= 35 && ch <=37 ) continue;

                if (ch < 30)
                    cm = ch;
                else
                    cm = ch - 2;

                volt = data[ch];

                if (volt < volt_min) volt_min = volt;
                if (volt > volt_max) volt_max = volt;

                float difference = volt_max - volt_min;
                int normvolt = (difference == 0)?0:(int)Math.Round(100*(volt - volt_min) / difference);

                string text = Math.Round((double)volt, 2).ToString();
                drawMeter(g, device, cm % 3, (int)Math.Floor((double)cm / 3), 100, 20, normvolt, text);
            }
        }

        void DrawGrid(Graphics g)
        {
            g.DrawLine(pens["Grey"], 20, 0, 20, 400);
            g.DrawLine(pens["Grey"], 370, 0, 370, 400);

            for (int i=1;i<21;i++) {
                g.DrawString(i.ToString(), f, brushes["Grey"], 0, i * 20 - 16);
                g.DrawString((20+i).ToString(), f, brushes["Grey"], 350, i * 20 - 16);
            }
        }


        void drawMeter(Graphics g, int device, int cell_x, int cell_y, int width, int height, double normvolt, string text)
        {
            Pen pen = pens["Grey"];
            Brush brush1 = brushes["Transparent"];
            Brush brush2 = brushes["Transparent"];
            Brush brush3 = brushes["Transparent"];

            int offset = device * PANEL_OFFSET;

            if (normvolt < 33)
            {
                brush1 = brushes["Green"];
            }
            else if (normvolt > 67)
            {
                brush3 = brushes["Red"];
            }
            else
            {
                brush2 = brushes["Amber"];
            }

            int rad = 12;
            int x = offset + 40 + OFFSET_X + cell_x * width;
            int y = OFFSET_Y + cell_y * height;

            g.FillEllipse(brush1, x, y + HEIGHT / 2, rad, rad);
            g.FillEllipse(brush2, x + rad + 1, y + HEIGHT / 2, rad, rad);
            g.FillEllipse(brush3, x + 2 * rad + 2, y + HEIGHT / 2, rad, rad);

            g.DrawEllipse(pen, x, y + HEIGHT / 2, rad, rad);
            g.DrawEllipse(pen, x + rad + 1, y + HEIGHT / 2, rad, rad);
            g.DrawEllipse(pen, x + 2 * rad + 2, y + HEIGHT / 2, rad, rad);

            g.DrawString(text, f, brushes["Black"], x - 45, y + 4);

        }


    }
}
