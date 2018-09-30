using System.Drawing;
using System.Windows.Forms;
using NPlot;
using System.Collections.Generic;
using System.Threading;

using DEVICEID = System.Int16;
//The data structure is a dictionary; K: device id V:raw list of data for device
//K: device id :. data imported by id
using DATA = System.Collections.Generic.Dictionary<System.Int16, System.Collections.Generic.List<int>>;

namespace MultiDeviceAIO
{
    public partial class FmScope : Form
    {
        //Display
        NPlot.LinePlot npplot;

        DATA data;
        int n_channels;
        int n_samples;

        //Data
        float[] dataX;
        float[] dataY;
        
        public FmScope(string filename)
        {
            //Isolate importing from main program.
            //Main program state is for current test setup!

            DATA concatdata;
            LoggerState sd;
            try
            {
                string header = IO.ReadFileHeader(filename);

                sd = PersistentLoggerState.LoadHeader(header);

                this.n_channels = sd.n_channels;

                //You will need to inject settings dependency into IO to decode the header
                //However you are splitting the file by width which is settings dependent
                //TODO: think about lose coupling IO and file format
                IO.ReadCSV<int>(filename, IO.DelegateParseInt<int>, out List<List<int>> jjdarray, ',', true);

                //TODO: get n_channels from header
                //TODO: move from IO since this is a file conversion now!

                IO.ConvertJJD2DATA(jjdarray, this.n_channels, out concatdata);
            }
            catch (System.Exception ex)
            {
                if (ex is System.IO.IOException ||
                    ex is System.FormatException)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                throw;
            }

            n_channels = sd.n_channels;

            //Must have some devices
            float n_devices = concatdata.Count;
            if (n_devices == 0) return;
            
            //Should match header ?check
            if (System.Math.Floor(n_devices) != n_devices) return;       //devices not multiple of data width

            Import(concatdata, sd.duration);

        }

        public FmScope(DATA concatdata, int n_channels, int duration)
        {
            this.n_channels = n_channels;
            Import(concatdata, duration);
        }

        protected void Import(DATA concatdata, int duration)
        {            
            InitializeComponent();

            if (concatdata == null || concatdata.Count < 2)
            {
                MessageBox.Show("No data");
                this.Close();
            }
            else
            {

                this.data = concatdata;
                //assume both devices same size
                this.n_samples = (concatdata.Count == 0) ? 0 : concatdata[0].Count / n_channels;

                if (this.n_samples == 0) return;

                npplot = new LinePlot();
                dataX = new float[n_samples];
                dataY = new float[n_samples];

                //Combo items
                List<string> options = new List<string>();
                for (int i = 0; i < n_channels; i++)
                {
                    options.Add("Channel " + (i + 1));
                }
                comboBox1.DataSource = options;
                comboBox1.SelectedIndex = 0;

                List<string> devices = new List<string>();
                for (int i = 0; i < concatdata.Count; i++)
                {
                    devices.Add("Device " + (i + 1));
                }
                comboBox2.DataSource = devices;
                comboBox2.SelectedIndex = 0;

                //Add event handler here to stop being called before initialised
                comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
                comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

                //X values
                float scale = (float)duration / (float)n_samples;
                for (int i = 0; i < n_samples; i++)
                {
                    dataX[i] = i * scale;
                }

                SetChannel(0, 0);

                Cursor.Current = Cursors.Arrow;
            }
        }

        void SetChannel(DEVICEID device, int channel)
        {
            float min = 10;
            float max = -10;

            int c = 0;
            for (int i = channel; i < data[device].Count; i+=n_channels)
            {
                dataY[c] = (float)data[device][i] / 65535 * 20 - 10;
                if (dataY[c] > max) max = dataY[c];
                if (dataY[c] < min) min = dataY[c];
                c++;
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
            SetChannel((DEVICEID) comboBox2.SelectedIndex, comboBox1.SelectedIndex);

            npSurface.Refresh();
        }

        private void comboBox2_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            SetChannel((DEVICEID) comboBox2.SelectedIndex, comboBox1.SelectedIndex);

            npSurface.Refresh();
        }
    }
}
