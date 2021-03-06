﻿using System;
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
        public static string fnMAPPING = @"mapping.csv";

        static Font f = new Font(FontFamily.GenericMonospace, 10);
        static SizeF rectAccLabel;
        static SizeF rectMeterLabel;

        Bitmap chVolt;
        Graphics g;
        
        private static readonly Dictionary<string, Pen> pens
            = new Dictionary<string, Pen>
            {
                { "Black", new Pen(Color.Black) },
                { "Transparent", new Pen(Color.Transparent) },
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
                { "Blue", new SolidBrush(Color.Blue) },
                { "DarkRed", new SolidBrush(Color.DarkRed) },
                { "DarkAmber", new SolidBrush(Color.DarkGoldenrod) },
                { "DarkGreen", new SolidBrush(Color.DarkGreen) },
                { "DarkGrey", new SolidBrush(Color.DarkGray) }
        };

        public Monitor(short n_channels)
        {
            InitializeComponent();

            chVolt = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            pictureBox1.Image = chVolt;

            g = Graphics.FromImage(chVolt);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            rectAccLabel = g.MeasureString("00", f);
            rectMeterLabel = g.MeasureString("-10.00", f);

            ReDraw();
        }

        ~Monitor()
        {
            g.Dispose();
        }

        public static void LoadAccelometerCalibration()
        {
            //Load calibration   
            using (var ofd = new OpenFileDialog())
            {
                DialogResult result = ofd.ShowDialog();

                if (result == DialogResult.OK && !ofd.FileName.IsNullOrWhiteSpace())
                {
                    //try
                    try
                    {
                        IO.ReadCSV<double>(ofd.FileName, IO.DelegateParseDouble<double>, out List<List<double>> caldata);
                        PersistentLoggerState.ps.data.caldata = caldata;
                        Accelerometer.ImportCalibration(PersistentLoggerState.ps.data.caldata);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show("Error loading file" + ex.Message);
                    }

                    MessageBox.Show("Cal file imported");
                }
            }
        }

        public static void LoadAccelometerSetup()
        {
            //Load calibration   
            using (var ofd = new OpenFileDialog())
            {
                DialogResult result = ofd.ShowDialog();

                if (result == DialogResult.OK && !ofd.FileName.IsNullOrWhiteSpace())
                {
                    //try
                    try
                    {
                        IO.ReadCSV<int>(ofd.FileName, IO.DelegateParseInt<int>, out List<List<int>> accsetup);
                        Monitor.ExtractSetupFile(accsetup);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show("Error loading file" + ex.Message);
                    }

                    MessageBox.Show("Setup file imported");
                }
            }
        }

        private static void ExtractSetupFile(List<List<int>> setup)
        {
            //spacings
            //1,2 = l/r
            //#
            //orientation number {1,2}
            Dictionary<string, List<int>> info = new Dictionary<string, List<int>>();
            List<List<int>> mapping = new List<List<int>>();
            const int OFFSET = 4;       //zero based
            int num;

            //extract other info
            num = setup[0].Count;
            if (setup[1].Count != num || setup[2].Count != num || setup[3].Count != num)
            {
                throw new FileFormatException("Accelerometer Setup File error: columns unequal");
            }

            info["spacing"] = new List<int>(setup[0]);
            info["position"] = new List<int>(setup[1]);
            info["number"] = new List<int>(setup[2]);
            info["orientation"] = new List<int>(setup[3]);

            info["spacing"].AddRange(setup[0]);                 //same spacings on second list
            info["position"].AddRange(setup[num + OFFSET]);
            info["number"].AddRange(setup[num + OFFSET + 1]);
            info["orientation"].AddRange(setup[num + OFFSET + 2]);

            for(int row = 0;row < num; row++)
            {
                mapping[row] = new List<int>();
                //add acc number
                mapping[row].Add(info["number"][row]);
                mapping[row].AddRange(setup[OFFSET + row]);
            }

            for (int row = 0; row < num; row++)
            {
                mapping[num + row] = new List<int>();
                //add acc number
                mapping[num + row].Add(info["number"][num + row]);
                mapping[num + row].AddRange(setup[OFFSET + 3 + num + row]);     //second offset is 1 less as no spacing
            }

            //TODO: store this
            //TODO: Test
        }

        void DrawAccelerometers(Rectangle rect)
        {
            //Takes channel data + cal_data and organises a display of accelerometers

            //in 2 columns (?make dynamic on resize) width window?
            //An acceleromoeter is 3x channels (with 3 states). 3x METER_WIDTH = 100

            //below on left, equal+ on right (1 based not 0 -> +1) = y num accelerometers
            int mid_acc = (int)Math.Floor((double)(Accelerometer.Count)/ 2);

            Rectangle rectAcc = new Rectangle();
            rectAcc.Width = rect.Width/2 - 2;
            rectAcc.Height = rect.Height / mid_acc - 7;

            int row, col;

            //41,42 are treated differently
            for (int a = 0; a< Accelerometer.accrs.Count - 2;a++)
            {
                //Key is proper number (not this List row crap elsewhere). 1 based not 0
                col = (int)Math.Floor((double)a / (mid_acc-1));
                row = a % (mid_acc-1);

                rectAcc.X = rect.X + col * (rectAcc.Width + 3);
                rectAcc.Y = rect.Y + row * (rectAcc.Height + 7);
                DrawAccelerometer(g, rectAcc, Accelerometer.accrs[a+1], rectAccLabel, rectMeterLabel, f);
            }
            //41
            rectAcc.X = rect.X + 0 * (rectAcc.Width + 3);
            rectAcc.Y = rect.Y + 20 * (rectAcc.Height + 7);
            DrawAccelerometer(g, rectAcc, Accelerometer.accrs[41], rectAccLabel, rectMeterLabel, f);
            //42
            rectAcc.X = rect.X + 1 * (rectAcc.Width + 3);
            rectAcc.Y = rect.Y + 20 * (rectAcc.Height + 7);
            DrawAccelerometer(g, rectAcc, Accelerometer.accrs[42], rectAccLabel, rectMeterLabel, f);
        }

        static void DrawAccelerometer(Graphics g, Rectangle rect, Accelerometer a, SizeF rectAccLabel, SizeF rectMeterLabel, Font f)
        {
            const int CORNER_RADIUS = 8;

            string label = a.number.ToString();
            Rectangle rectLabel = new Rectangle() { X = rect.X, Y = rect.Y + (int)(rect.Height - rectAccLabel.Height) / 2, Width = (int)rectAccLabel.Width , Height = (int)rectAccLabel.Height };

            //g.DrawRoundedRectangle(pens["Grey"], rect, CORNER_RADIUS);
            g.FillRoundedRectangle(brushes["White"], rect, CORNER_RADIUS);

            g.FillRoundedRectangle(brushes["Black"], rectLabel, 2);
            g.DrawRoundedRectangle(pens["White"], rectLabel, 2);
            g.DrawString(label, f, brushes["White"], rectLabel.X, rectLabel.Y);

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

                drawMeter(g, rectMeter, a.channels[c], rectMeterLabel, f);
            }

        }

        static void drawMeter(Graphics g, Rectangle rect, Channel c, SizeF rectMeterLabel, Font f)
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

            rect.Width -= (int)rectMeterLabel.Width;
            rect.X += (int)rectMeterLabel.Width;
            int radius = 7;

            Pen pen = pens["Grey"];
            Brush brush1 = brushes["Transparent"];
            Brush brush2 = brushes["Transparent"];
            Brush brush3 = brushes["Transparent"];

            switch (c.State)
            {
                case 1:
                    brush1 = brushes["Red"];
                    break;
                case 2:
                    brush1 = brushes["Green"];
                    break;
                case 4:
                    brush1 = brushes["Red"];
                    brush2 = brushes["Red"];
                    break;
                case 8:
                    brush2 = brushes["Green"];
                    break;
                case 16:
                    brush2 = brushes["Red"];
                    brush3 = brushes["Red"];
                    break;
                case 32:
                    brush3 = brushes["Green"];
                    break;
                case 64:
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

        static void drawLed(Graphics g, Brush brush, Pen pen, Rectangle rect)
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
        //externally trigger redraw
        public void ReDraw()
        {
            g.Clear(Color.Transparent);

            //DrawGrid(g);

            DrawAccelerometers(
                new Rectangle() {X = 0,Y = 0,Width = chVolt.Width,Height = chVolt.Height}
                );

            //if (aio.devicenames.Count > 0) DrawDeviceChannels(0, snapshot[0]);
            //if (aio.devicenames.Count > 1) DrawDeviceChannels(1, snapshot[1]);

            pictureBox1.Image = chVolt;
        }

        protected override void OnShown(EventArgs e)
        {
            //Caller has set the aio object now
            base.OnShown(e);
        }

    }
}
