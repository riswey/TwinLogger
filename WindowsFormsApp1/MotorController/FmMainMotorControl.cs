using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MotorController;

namespace MultiDeviceAIO
{
    public partial class FmMain: Form
    {

#if SOFTDEVICE
    TestSerialPort serialPort1;
#else
        SerialPort serialPort1 = new SerialPort();

        SerialLayer serial = new SerialLayer();
#endif
        string TERMINAL = "\n";

        StateMachine rotorstate = new StateMachine("ArduinoState", ARDUINOSTATE.Ready);

        private DataTable _dt;
        public DataTable dt
        {
            get
            {
                if (_dt == null)
                {
                    _dt = new DataTable();
                    _dt.Columns.Add("X_Value", typeof(long));
                    _dt.Columns.Add("Target", typeof(float));
                    _dt.Columns.Add("Upper", typeof(float));
                    _dt.Columns.Add("Lower", typeof(float));
                    _dt.Columns.Add("Y_Value", typeof(float));
                }
                return _dt;
            }
        }

        private void MotorCleanUp()
        {
            SendCommand(CMD.STOP);
            serialPort1?.Close();
            serialPort1?.Dispose();
        }

        //Encapsulating all trigger activity and event calls here
        //Control is handed to Rotor when appstate = Armed, rotorstate = Running
        TriggerLogic trigger;

        //called by ControlPanel constructor
        public void InitFmCPMotorControl()
        {
            LoggerState ls = PersistentLoggerState.ps.data;
            //Called by LoggerState constructor
            int tickpermetricwindow = (int)Math.Ceiling((double)ls.metric_window / ls.arduinotick);
            MovingStatsCrosses mac = new MovingStatsCrosses(tickpermetricwindow, () => { return ls.target_speed; });
            trigger = new TriggerLogic(mac, ls);

            //Setup Chart
            chart1.Titles.Add("Rotor Trajectory");

            chart1.ChartAreas["ChartArea1"].AxisX.MajorGrid.LineColor = Color.Gainsboro;
            chart1.ChartAreas["ChartArea1"].AxisY.MajorGrid.LineColor = Color.Gainsboro;
            chart1.Series.Add(new Series("Series2"));
            chart1.Series.Add(new Series("Series3"));
            chart1.Series.Add(new Series("Series4"));

            foreach (Series series in chart1.Series)
            {
                series.IsVisibleInLegend = false;
                series.XValueMember = "X_Value";
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            }
            chart1.Series["Series1"].YValueMembers = "Target";
            chart1.Series["Series2"].YValueMembers = "Upper";
            chart1.Series["Series3"].YValueMembers = "Lower";
            chart1.Series["Series4"].YValueMembers = "Y_Value";

            chart1.Series["Series1"].Color = Color.Black;
            chart1.Series["Series2"].Color = Color.Red;
            chart1.Series["Series3"].Color = Color.Red;
            chart1.Series["Series4"].Color = Color.Blue;

            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "";

            //chart1.Series[0].Points.Add(new double[] { 1, 5, 3, 7, 20, 50, 30 } );

            chart1.ChartAreas[0].AxisX.Minimum = 0;
            //chart1.ChartAreas[0].AxisX.Maximum = 100;

            chart1.DataSource = dt;

            UpdateChartYScale();

            //Setup Serial Port
#if SOFTDEVICE
            serialPort1 = new TestSerialPort(this);
#else
            serialPort1 = new SerialPort();
#endif

            string pn = SearchPorts();

            if (pn == "(None)") return;

            serialPort1.PortName = pn;

            serialPort1.BaudRate = 9600;
            serialPort1.Open();
            serialPort1.DiscardInBuffer();  //clear anything


#if SOFTDEVICE
            //TODO: Testing should really have a delegate handling this!
#else
            serialPort1.DataReceived += serialPort1_DataReceived;
#endif

            //TODO: explicitly/clearly put this in the startup sequence
        }

        void BindMotorControls()
        {
            nudMetricWindow.DataBindings.Clear();
            nudMetricWindow.DataBindings.Add("Value", PersistentLoggerState.ps.data, "metric_window");

            nudTargetSpeed.DataBindings.Clear();
            nudTargetSpeed.DataBindings.Add("Value", PersistentLoggerState.ps.data, "target_speed");

            lblCurrentSpeed.DataBindings.Clear();
            lblCurrentSpeed.DataBindings.Add("Text", PersistentLoggerState.ps.data, "rotor_speed");

            nudP.DataBindings.Clear();
            nudP.DataBindings.Add("Value", PersistentLoggerState.ps.data, "p");

            nudI.DataBindings.Clear();
            nudI.DataBindings.Add("Value", PersistentLoggerState.ps.data, "i");

            nudD.DataBindings.Clear();
            nudD.DataBindings.Add("Value", PersistentLoggerState.ps.data, "d");

            nudTimeout.DataBindings.Clear();
            nudTimeout.DataBindings.Add("Value", PersistentLoggerState.ps.data, "timeout");

            txtMetricCommand.DataBindings.Clear();
            txtMetricCommand.DataBindings.Add("Text", PersistentLoggerState.ps.data, "metriccommand");
            
            //Bind MAC Stats
            lblMA.DataBindings.Clear();
            lblMA.DataBindings.Add("Text", trigger.mac, "MA");
            lblSTD.DataBindings.Clear();
            lblSTD.DataBindings.Add("Text", trigger.mac, "STD");
            lblGrad.DataBindings.Clear();
            lblGrad.DataBindings.Add("Text", trigger.mac, "Gradient");
            lblCross.DataBindings.Clear();
            lblCross.DataBindings.Add("Text", trigger.mac, "Crosses");
            lblMin.DataBindings.Clear();
            lblMin.DataBindings.Add("Text", trigger.mac, "Min");
            lblMax.DataBindings.Clear();
            lblMax.DataBindings.Add("Text", trigger.mac, "Max");

        }

        private string SearchPorts()
        {
            string[] ports = SerialPort.GetPortNames();

            cbxPort.Items.Clear();

            foreach (string port in ports)
            {
                cbxPort.Items.Add(port);
            }

            if (cbxPort.Items.Count > 0)
            {
            }
            else
            {
                cbxPort.Items.Add("(None)");
            }

            cbxPort.SelectedIndex = cbxPort.Items.Count - 1;

            return cbxPort.SelectedItem.ToString();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //TODO: stop motor then close serial port
            StopSeries("Closing");
            //if (task != null) task.Dispose();
            //if (serialPort1 != null) serialPort1.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Get current device state
            SendCommand(CMD.GETPID);
        }


        void SET_LABEL(string idx)
        {
            AsyncText(lblRotorState, idx.ToString());
        }

        void SendCommand(CMD cmd)
        {
            string data = "";
            switch ((CMD)cmd)
            {
                case CMD.SETFREQ:
                    data = nudTargetSpeed.Value.ToString();
                    break;
/*                case CMD.SETPULSEDELAY:
                    //data = nudDesireSpeed.Value.ToString();
                    break;*/
                    /*
                case CMD.SETPID:
                    data = nudP.Value.ToString();
                    data += " " + nudI.Value.ToString();
                    data += " " + nudD.Value.ToString();
                    break;
                    */
//NEW
                case CMD.SETADC:
                    //TODO: get the value of the clock freq
                    //data = nudP.Value.ToString();
                    data = "1000";
                    break;

            }

            doSend(cmd, data);

        }

        //Means can hijack for meta motor ctrl
        void doSend(CMD cmd, string data)
        {
            Enums.CMDEncode.TryGetValue(cmd, out string strcmd);
            string packet = strcmd + " " + data + TERMINAL;

            AsyncText(tbxHistory, packet + "\t" + cmd.ToString() + "\r\n", -1);

            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Write(packet);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }

        void AbortRun()
        {
            SendCommand(CMD.STOP);
        }

        //TODO: Made public for testing
        public void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                string packet = serialPort1.ReadLine();
                string trimmed = packet.TrimEnd(Environment.NewLine.ToCharArray());
                HandlePacket(trimmed);
            }
        }

        void HandlePacket(string packet)
        {
            //NOTE: Updating settings -> GUI update and cross thread error 
            //TODO: can we move the main thread load later in process

            if (InvokeRequired)
            {
                this.Invoke(new Action(() => HandlePacket(packet) ));
                return;
            }

            AsyncText(tbxHistory, packet + "\r\n", -1);

            //New - API has no space in ACK
            if (packet.Substring(0, 3) == "ACK" && packet.Substring(0, 4) != "ACK ")
            {
                packet = packet.Substring(0, 3) + " " + packet.Substring(3);
            }

            string[] data = packet.Split(' ');

            Enums.DATATYPEDecode.TryGetValue(data[0], out DATATYPES cmd);

            //Unknown packet
            if (cmd == null)
            {
                AsyncText(tbxHistory, "Output: " + packet + "\r\n", -1);
                return;
            }
            
            switch (cmd)
            {
                case DATATYPES.ACK:
                    //ACK events
                    Enums.ACKDecode.TryGetValue(data[1], out ARDUINOEVENT ack_event);
                    rotorstate.Event(ack_event);
                    break;
                    /*
                case DATATYPES.GETPULSEDELAY:
                    PersistentLoggerState.ps.data.pulse_delay = int.Parse(data[1]);
                    break;
                    */
                case DATATYPES.GETTARGETFREQ:
                    PersistentLoggerState.ps.data.target_speed = float.Parse(data[1]);
                    break;
                case DATATYPES.GETROTORFREQ:
                    //NOTE: Rotor is set here
                    float rs = float.Parse(data[1]);
                    float ts = PersistentLoggerState.ps.data.target_speed;
                    int rx = (int)PersistentLoggerState.ps.data.RotorX;
                    //Rotor Speed fork in storage (guaranteed sync as flows from here only)
                    PersistentLoggerState.ps.data.rotor_speed = rs;             //bound the controls
                    trigger.mac.Add(rs);                                        //meta data + trigger
                    dt.Rows.Add(rx, ts, ts + 5, ts - 5, rs);
                    break;
/*                case DATATYPES.GETMINMAXPERIODS:
                    PersistentLoggerState.ps.data.min_period = long.Parse(data[1]);
                    PersistentLoggerState.ps.data.max_period = long.Parse(data[2]);
                    break;
                    */
                case DATATYPES.GETPID:
                    PersistentLoggerState.ps.data.p = float.Parse(data[1]);
                    PersistentLoggerState.ps.data.i = float.Parse(data[2]);
                    PersistentLoggerState.ps.data.d = float.Parse(data[3]);
                    break;
                case DATATYPES.GETLOCKABLE:
                    if (data[1] == "Y")
                    {
                        /* This is a weird one.
                         * Making it unlockable is exacly the same process as Unlock
                         * Yet normally Unlock is prompted by a START press
                         * -> Serial communication by the Event handler
                         * But here the Serial Comms is result of an RL command
                         * So if true -> just need to Simulate the ACK of an SU (set unlock)
                         */
                         rotorstate.Event(ARDUINOEVENT.Do_unlock);
                    }
                    break;
//NEW
                case DATATYPES.GETADC:
                    //MessageBox.Show("ADC SET: " + float.Parse(data[1]));
                    break;
                //END

                default:
                    AsyncText(toolStripStatusLabel1, "Unknown Packet: " + packet);
                    break;
            }
        }

        private void serialPort1_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show(e.ToString());
        }

        private void btnSetSpeed_Click(object sender, EventArgs e)
        {
            SendCommand(CMD.SETFREQ);
        }

        protected void nudP_ValueChanged(object sender, EventArgs e)
        {
            //SendCommand(CMD.SETPID);
        }

        private void nudI_ValueChanged(object sender, EventArgs e)
        {
            //SendCommand(CMD.SETPID);
        }

        private void nudD_ValueChanged(object sender, EventArgs e)
        {
            //SendCommand(CMD.SETPID);
        }

        private void AsyncText(Control obj, string text, int append = 0)
        {
            if (append == 0)
            {
                obj.Invoke(new Action<string>(n => obj.Text = n), new object[] { text });
            }
            else if (append == 1)
            {
                obj.Invoke(new Action<string>(n => obj.Text += n), new object[] { text });
            }
            else
            {
                obj.Invoke(new Action<string>(n => obj.Text = n + obj.Text), new object[] { text });
            }
        }

        private void AsyncText(ToolStripLabel obj, string text)
        {
            obj.GetCurrentParent().Invoke(new Action<string>(n => obj.Text = n), new object[] { text });
        }

        private void AsyncNUD(NumericUpDown obj, decimal value)
        {
            obj.Invoke(new Action<decimal>(n => { obj.Value = value; obj.Refresh(); }), new object[] { value });
        }

        private void AsyncColor(Control obj, Color color)
        {
            obj.Invoke(new Action(() => obj.BackColor = color));
        }

        private void AsyncDisable(Control obj, bool disable = true)
        {
            obj.Invoke(new Action(() => obj.Enabled = !disable));
        }

        public void Msg(string msg)
        {
            MessageBox.Show(msg);
        }

        //TODO: log path
        /* What is this
        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (tbxLogPath.Text.Trim() != "")
                {
                    fbd.SelectedPath = tbxLogPath.Text;
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    tbxLogPath.Text = fbd.SelectedPath;
                }

            }
        }
        */

        private void btnReset_Click(object sender, EventArgs e)
        {
            rotorstate.Event(ARDUINOEVENT.Send_Stop);
            rotorstate.Event(ARDUINOEVENT.Do_Stop);
        }

        private void graphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not implemented yet");
        }

        private void cbxPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.PortName = cbxPort.SelectedItem.ToString();
        }

        int timercycle = 0;

        public void serialpoller_Tick(object sender, EventArgs e)
        {
            if (rotorstate.state == ARDUINOSTATE.Running.ToString())
            {
                //If started but not entered lock cycle yet -> poll to see if lockable
                SendCommand(CMD.GETLOCKABLE);
            }

            SendCommand(CMD.GETROTORFREQ);

            //Log if 4th tick AND Max/Min are meaningful
            if ((timercycle = (++timercycle) % 4) == 0)
            {
                //if (PersistentLoggerState.ps.data.IsRMDisabled())
                //{
                    //TODO: ensure that RM min/max period is scrubbed - no longer needed
                    //RM (min/max) disable period expired
//                    SendCommand(CMD.GETMINMAXPERIODS);
//                    SendCommand(CMD.GETPULSEDELAY);
                    SendCommand(CMD.GETPID);
                    //SendCommand(CMD.GETTARGETFREQ);
                    PersistentLoggerState.ps.data.LogWrite();
                //}
            }

            //New
            //It is effectively queued to Arduino altho async return
            
            chart1.DataBind();

            if (!appstate.IsState(APPSTATE.DoSampling) && trigger.IsReadyToSample)
            {
                cbxOK.Checked = true;
                appstate.Event(APPEVENT.Trigger);
            }
            else
                cbxOK.Checked = false;
        }

        private void UpdateChartYScale()
        {
            chart1.ChartAreas[0].AxisY.Maximum = PersistentLoggerState.ps.data.target_speed + graphrange;
            chart1.ChartAreas[0].AxisY.Minimum = PersistentLoggerState.ps.data.target_speed - graphrange;
        }

    }
}
