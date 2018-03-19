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

        //Data
        float[] dataX;
        float[] dataY;

        public Scope(List<int> dataY1, int n_channels)
        {
            int size = dataY1.Count / n_channels;

            dataY = new float[size];
            float min = 10;
            float max = -10;

            int c = 0;
            for (int i=0;i<dataY1.Count; i++)
            {
                if (i % n_channels == 0)
                {
                    dataY[c] = (float)dataY1[i] / 65535 * 20 - 10;
                    if (dataY[c] > max) max = dataY[c];
                    if (dataY[c] < min) min = dataY[c];
                    c++;
                }
            }
            //Take range away from limits
            max *= 1.2f;
            min *= 1.2f;
            //check range for zero
            if ((max - min) < 1) {
                min -= .5f;
                max += .5f;
            }

            InitializeComponent();
            initData();
            CreateLineGraph(min, max);
        }

        private void initData()     //for testing
        {
            npplot = new LinePlot();

            dataX = new float[dataY.Length];

            for (int i = 0; i < dataY.Length; i++)
            {
                dataX[i] = i;
            }
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

    }
}
