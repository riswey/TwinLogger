using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using System.Diagnostics;

using DEVICEID = System.Int16;

namespace MultiDeviceAIO
{
    public partial class Monitor : Form
    {
        public MyAIO aio;

        string fnMAPPING = @"mapping.csv";

        //TODO: this needs to be set in settings!
        string fnCalibration = null;

        short n_channels;

        Font f = new Font(FontFamily.GenericMonospace, 10);
        SizeF rectAccLabel;
        SizeF rectMeterLabel;

        Bitmap chVolt;
        Graphics g;
        
        private static readonly Dictionary<string, Pen> pens
            = new Dictionary<string, Pen>
            {
                { "Black", new Pen(Color.Black) },
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
                { "DarkGreen", new SolidBrush(Color.DarkGreen) },
                { "DarkGrey", new SolidBrush(Color.DarkGray) }
        };

        public Monitor(MyAIO aio, short n_channels)
        {
            InitializeComponent();

            this.aio = aio;
            this.n_channels = n_channels;

            chVolt = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            pictureBox1.Image = chVolt;

            g = Graphics.FromImage(chVolt);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            rectAccLabel = g.MeasureString("00", f);
            rectMeterLabel = g.MeasureString("-10", f);

            List<List<int>> mapping = null;
            try
            {
                IO.ReadCSV<int>(fnMAPPING, IO.DelegateParseInt<int>, out mapping, ',', true);
            } catch (FileNotFoundException e)
            {
                throw new Exception("Cannot find mapping.csv");
            }

            Accelerometer.ImportMapping(mapping, n_channels);

            //If not set just returns false
            ImportCalibrationFile();

            timer1.Start();
        }


        ~Monitor()
        {
            timer1.Dispose();
            g.Dispose();
        }

        //TODO: sort out error handling. Magic number rather than create new exception type!
        //Currently false means no file set
        //What about CSV errors!
        bool ImportCalibrationFile()
        {
            if (fnCalibration == null)
            {
                return false;
            }

            //try
            IO.ReadCSV<double>(fnCalibration, IO.DelegateParseDouble<double>, out List<List<double>> caldata);

            foreach(KeyValuePair<int, Accelerometer> accr in Accelerometer.accrs)
            {
                accr.Value.Calibrate(caldata);
            }

            MessageBox.Show("Cal file imported");

            return true;
        }

        /*
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
        */
        

        //TODO: should send a rectange to next layer to work within

        
        void DrawAccelerometers(Rectangle rect)
        {

            //Takes channel data + cal_data and organises a display of accelerometers

            //in 2 columns (?make dynamic on resize) width window?
            //An acceleromoeter is 3x channels (with 3 states). 3x METER_WIDTH = 100

            //below on left, equal+ on right (1 based not 0 -> +1) = y num accelerometers
            int mid_acc = (int)Math.Floor((double)Accelerometer.Count / 2);

            Rectangle rectAcc = new Rectangle();
            rectAcc.Width = rect.Width/2;
            rectAcc.Height = rect.Height / mid_acc;

            foreach (KeyValuePair<int, Accelerometer> accr in Accelerometer.accrs)
            {
                //Key is proper number (not this List row crap elsewhere). 1 based not 0
                rectAcc.X = rect.X + (int)Math.Floor((double)(accr.Key-1) / mid_acc) * rectAcc.Width;
                rectAcc.Y = rect.Y + (accr.Key-1) % mid_acc * rectAcc.Height;
                DrawAccelerometer(rectAcc, accr.Value);
            }

        }
        
        void DrawAccelerometer(Rectangle rect, Accelerometer a )
        {

            string label = a.number.ToString();
            Rectangle rectLabel = new Rectangle() { X = rect.X, Y = rect.Y, Width = (int)rectAccLabel.Width , Height = (int)rectAccLabel.Height };
            
            g.FillEllipse(brushes["White"], rectLabel);
            g.DrawEllipse(pens["White"], rectLabel);

            g.DrawString(label, f, brushes["Grey"], rect.X, rect.Y);

            int newWidth = rect.Width - (int)rectAccLabel.Width;

            //Controls drawMeter for x,y,z linearly
            int meter_width = newWidth / 3;

            Rectangle rectMeter = new Rectangle() { Y = rect.Y, Width = newWidth / 3, Height = rect.Height};
            for (int c = 0; c < 3; c++)
            {
                //int x = offset + 40 + OFFSET_X + cell_x * width;
                //int y = OFFSET_Y + cell_y * height;

                //currently cm % 3 (so x = 0,1,2)
                //(int)Math.Floor((double)cm / 3) (y = 0,1,2...)

                rectMeter.X = rect.X + (int)rectAccLabel.Width + c * meter_width;

                drawMeter(g, rectMeter, a.channels[c]);
            }

        }

        void drawMeter(Graphics g, Rectangle rect, Channel c)
        {
            rect.X += 1;
            rect.Y += 1;
            rect.Width -= 2;
            rect.Height -= 2;

            //g.DrawRoundedRectangle(pens["Grey"], rect, 6);
            //g.FillRoundedRectangle(brushes["DarkGrey"], rect, 6);

            //TODO: better quality but takes dc
            //TextRenderer.DrawText(g.GetHdc(), c.G.ToString(), f, rect.X, rect.Y, brushes["Black"]);
            g.DrawString(c.G.ToString(), f, brushes["Black"], rect.X, rect.Y);

            rect.Width -= (int)rectAccLabel.Width;
            rect.X += (int)rectAccLabel.Width;
            int radius = 7;

            Pen pen = pens["Grey"];
            Brush brush1 = brushes["Transparent"];
            Brush brush2 = brushes["Transparent"];
            Brush brush3 = brushes["Transparent"];

            switch (c.State)
            {
                case 0:
                    brush1 = brushes["Green"];
                    break;
                case 1:
                    brush2 = brushes["Amber"];
                    break;
                case 2:
                    brush3 = brushes["Red"];
                    break;
            }

            rect.Width = radius * 2;
            rect.Height = radius * 2;
            drawLed(g, brush1, pen, rect);

            rect.X += 2*radius;
            drawLed(g, brush2, pen, rect);

            rect.X += 2*radius;
            drawLed(g, brush3, pen, rect);

        }

        void drawLed(Graphics g, Brush brush, Pen pen, Rectangle rect)
        {
            g.FillEllipse(brush, rect);
            g.DrawEllipse(pen, rect);
        }
        /*
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
        */

        void DrawGrid(Graphics g)
        {
            g.DrawLine(pens["Grey"], 20, 0, 20, 400);
            g.DrawLine(pens["Grey"], 370, 0, 370, 400);

            for (int i = 1; i < 21; i++)
            {
                g.DrawString(i.ToString(), f, brushes["Grey"], 0, i * 20 - 16);
                g.DrawString((20 + i).ToString(), f, brushes["Grey"], 350, i * 20 - 16);
            }
        }

        //Events
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Load calibration   
            using (var ofd = new OpenFileDialog())
            {
                DialogResult result = ofd.ShowDialog();

                if (result == DialogResult.OK && !ofd.FileName.IsNullOrWhiteSpace())
                {
                    //automatically set cal data
                    fnCalibration = ofd.FileName;

                    if (!ImportCalibrationFile())
                    {
                        MessageBox.Show("Calibration file not set.");
                    }
                }
            }

        }

        protected override void OnShown(EventArgs e)
        {
            //Caller has set the aio object now
            base.OnShown(e);
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            g.Clear(Color.Transparent);

            //DrawGrid(g);

            List<int[]> snapshot = aio.ChannelsSnapShotBinary(n_channels);

            Accelerometer.setChannelData(snapshot);

            
            DrawAccelerometers(
                new Rectangle() {X = 0,Y = 0,Width = chVolt.Width,Height = chVolt.Height}
                );

            //if (aio.devicenames.Count > 0) DrawDeviceChannels(0, snapshot[0]);
            //if (aio.devicenames.Count > 1) DrawDeviceChannels(1, snapshot[1]);

            pictureBox1.Image = chVolt;
        }


    }
}
