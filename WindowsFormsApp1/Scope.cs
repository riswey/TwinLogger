using System.Drawing;
using System.Windows.Forms;
using NPlot;
using System.Collections.Generic;

namespace MultiDeviceAIO
{
    public partial class Scope : Form
    {
        //Display
        NPlot.LinePlot npplot;

        List<List<int>> dataY1;
        int n_channels;
        int size;

        //Data
        float[] dataX;
        float[] dataY;

        public Scope(List<List<int>> dataY1, int n_channels)
        {
            this.dataY1 = dataY1;
            this.n_channels = n_channels;
            //assume both devices same size
            this.size = dataY1[0].Count / n_channels;

            InitializeComponent();

            npplot = new LinePlot();
            dataX = new float[size];
            dataY = new float[size];

            //Combo items
            List<string> options = new List<string>();
            for (int i = 0; i < n_channels; i++)
            {
                options.Add("Channel " + (i + 1));
            }
            comboBox1.DataSource = options;
            comboBox1.SelectedIndex = 0;

            List<string> devices = new List<string>();
            for (int i = 0; i < dataY1.Count; i++)
            {
                devices.Add("Device " + (i + 1));
            }
            comboBox2.DataSource = devices;
            comboBox2.SelectedIndex = 0;

            //Add event handler here to stop being called before initialised
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

            //X values
            for (int i = 0; i < size; i++)
            {
                dataX[i] = i;
            }

            SetChannel(0, 0);

        }

        void SetChannel(int device, int channel)
        {
            float min = 10;
            float max = -10;

            int c = 0;
            for (int i = 0; i < dataY1[device].Count; i++)
            {
                if (i % n_channels == channel)
                {
                    dataY[c] = (float)dataY1[device][i] / 65535 * 20 - 10;
                    if (dataY[c] > max) max = dataY[c];
                    if (dataY[c] < min) min = dataY[c];
                    c++;
                }
            }
            //Take range away from limits
            max *= 1.2f;
            min *= 1.2f;
            //check range for zero
            if ((max - min) < 1)
            {
                min -= .5f;
                max += .5f;
            }

            CreateLineGraph(min, max);
        }

        private void CreateLineGraph(float min, float max)
        {
            //Font definitions:
            Font TitleFont = new Font("Arial", 12);
            Font AxisFont = new Font("Arial", 10);
            Font TickFont = new Font("Arial", 8);

            //Legend definition:
            NPlot.Legend npLegend = new NPlot.Legend();

            //Prepare PlotSurface:
            //npSurface.Clear();
            npSurface.Title = "Channel Voltages";
            //npSurface.BackColor = System.Drawing.Color.AliceBlue;

            //Left Y axis grid:
            NPlot.Grid p = new Grid();
            npSurface.Add(p, NPlot.PlotSurface2D.XAxisPosition.Bottom,
                          NPlot.PlotSurface2D.YAxisPosition.Left);

            npplot.DataSource = dataY;
            npplot.Color = Color.Blue;
            npSurface.Add(npplot, NPlot.PlotSurface2D.XAxisPosition.Bottom,
                    NPlot.PlotSurface2D.YAxisPosition.Left);
            //Y axis

            npSurface.YAxis1.Label = "Voltage";
            npSurface.YAxis1.WorldMax = max;
            npSurface.YAxis1.WorldMin = min;
            npSurface.YAxis1.NumberFormat = "{0:####0.0}";
            npSurface.YAxis1.LabelFont = AxisFont;
            npSurface.YAxis1.TickTextFont = TickFont;

            //Update PlotSurface:
            npSurface.Refresh();

            //Save PlotSurface to MemoryStream, stream output as GIF file:
            /*Response.Buffer = true;
            Response.ContentType = "image/gif";

            MemoryStream memStream = new MemoryStream();

            npSurface.Bitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Gif);
            memStream.WriteTo(Response.OutputStream);
            Response.End();
            */

        }

        private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            SetChannel(comboBox2.SelectedIndex, comboBox1.SelectedIndex);

            npSurface.Refresh();
        }

        private void comboBox2_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            SetChannel(comboBox2.SelectedIndex, comboBox1.SelectedIndex);

            npSurface.Refresh();
        }
    }
}
