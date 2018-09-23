using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MotorController;

namespace MultiDeviceAIO
{
    public partial class FmControlPanel: Form
    {
        TestSerialPort serialPort1;
        //SerialPort serialPort1 = new SerialPort();

        string TERMINAL = "\n";
        STATE state = STATE.Ready;
        //Task task = null;

        //ParameterState parameters = new ParameterState();

        public void InitFmCPMotorControl()
        {
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

            chart1.DataSource = PersistentLoggerState.ps.data.dt;

            UpdateYScale();

            //Setup Serial Port
            serialPort1 = new TestSerialPort(this);
            //serialPort1 = new SerialPort();

            string pn = SearchPorts();

            if (pn == "(None)") return;

            serialPort1.PortName = pn;

            serialPort1.BaudRate = 9600;
            serialPort1.Open();
            serialPort1.DiscardInBuffer();  //clear anything

            //TODO: Testing should really have a delegate handling this!
            //serialPort1.DataReceived += serialPort1_DataReceived;

        }


        void BindMotorControls()
        {
            nudDesireSpeed.DataBindings.Clear();
            nudDesireSpeed.DataBindings.Add("Value", PersistentLoggerState.ps.data, "target_speed");

            lblCurrentSpeed.DataBindings.Clear();
            lblCurrentSpeed.DataBindings.Add("Text", PersistentLoggerState.ps.data, "rotor_speed");

            lblMinRotorPeriod.DataBindings.Clear();
            lblMinRotorPeriod.DataBindings.Add("Text", PersistentLoggerState.ps.data, "min_period");

            lblMaxRotorPeriod.DataBindings.Clear();
            lblMaxRotorPeriod.DataBindings.Add("Text", PersistentLoggerState.ps.data, "max_period");

            lblPulseDelay.DataBindings.Clear();
            lblPulseDelay.DataBindings.Add("Text", PersistentLoggerState.ps.data, "pulse_delay");

            nudP.DataBindings.Clear();
            nudP.DataBindings.Add("Value", PersistentLoggerState.ps.data, "p");

            nudI.DataBindings.Clear();
            nudI.DataBindings.Add("Value", PersistentLoggerState.ps.data, "i");

            nudD.DataBindings.Clear();
            nudD.DataBindings.Add("Value", PersistentLoggerState.ps.data, "d");

            nudTolerance.DataBindings.Clear();
            nudTolerance.DataBindings.Add("Value", PersistentLoggerState.ps.data, "tolerance");

            nudStableWindow.DataBindings.Clear();
            nudStableWindow.DataBindings.Add("Value", PersistentLoggerState.ps.data, "stableperiod");

            nudTimeout.DataBindings.Clear();
            nudTimeout.DataBindings.Add("Value", PersistentLoggerState.ps.data, "timeout");

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
            //if (task != null) task.Dispose();
            if (serialPort1 != null) serialPort1.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Get current device state
            SendCommand(CMD.GETPID);
            AsyncText(lblState, state.ToString());
        }

        private void ProcessEvent(EVENT e)
        {
            switch (e)
            {
                case EVENT.Start:
                    switch (state)
                    {
                        case STATE.Ready:
                            ChangeState(EVENT.Start);
                            break;
                        case STATE.Lockable:
                            ChangeState(EVENT.Lock);
                            break;
                        case STATE.Locked:
                            ChangeState(EVENT.Unlock);
                            break;
                    }
                    break;

                case EVENT.Lock:
                    switch (state)
                    {
                        case STATE.Lockable:
                            ChangeState(EVENT.Lock);
                            break;
                    }
                    break;
                case EVENT.Unlock:
                    switch (state)
                    {
                        case STATE.Locked:
                            ChangeState(EVENT.Unlock);
                            break;
                    }
                    break;
                case EVENT.Stop:
                    switch (state)
                    {
                        case STATE.Running:
                        case STATE.Lockable:
                        case STATE.Locked:
                            ChangeState(EVENT.Stop);
                            break;
                        //NEW
                        case STATE.Triggered:
                            ChangeState(EVENT.Stop);
                            break;

                    }
                    break;
                case EVENT.Trigger:
                    switch (state)
                    {
                        case STATE.Running:
                        case STATE.Lockable:
                        case STATE.Locked:
                            ChangeState(EVENT.Trigger);
                            break;
                    }
                    break;
            }

        }

        private void ChangeState(EVENT e)
        {
            PrintLn("Change Motor State: " + e.ToString());

            switch (e)
            {
                case EVENT.Start:
                    SendCommand(CMD.SETFREQ);
                    SendCommand(CMD.GETTARGETFREQ);
                    SendCommand(CMD.START);
                    break;
                case EVENT.Stop:
                    SendCommand(CMD.STOP);
                    break;
                case EVENT.Lock:
                    SendCommand(CMD.SETLOCK);
                    break;
                case EVENT.Unlock:
                    SendCommand(CMD.SETUNLOCK);
                    break;
                case EVENT.Trigger:
                    SendCommand(CMD.SETADC);
                    SendCommand(CMD.GETADC);
                    SendCommand(CMD.TRIGGER);
                    break;
            }
        }

        private void ProcessACK(CMD cmd)
        {
            PrintLn("Process ACK: " + cmd.ToString());

            switch (cmd)
            {
                case CMD.START:
                    //PersistentLoggerState.ps.data.Start();   //Removed Motor Control Logging
                    PersistentLoggerState.ps.data.StartRMTimer();
                    state = STATE.Running;
                    AsyncColor(btnStart, Color.Green);
                    //TODO: need check if lockable before lock when sampling!!!!
                    //Task task = Task.Delay(5000).ContinueWith(t => ProcessEvent(EVENT.Lock));
                    this.Invoke(new Action(() => timerarduino.Start()));
                    break;
                case CMD.STOP:
                    /*
                    if (this.task != null)
                    {
                        this.task.Dispose();
                        this.task = null;
                    }
                    */
                    this.Invoke(new Action(() => timerarduino.Stop()));
                    state = STATE.Ready;
                    //AsyncDisable(this.btnSetSpeed, false);
                    AsyncColor(btnStart, default(Color));
                    AsyncText(btnStart, "Start");
                    break;
                case CMD.SETLOCK:
                    state = STATE.Locked;
                    AsyncText(btnStart, "Unlock");
                    AsyncColor(btnStart, Color.Red);
                    //AsyncDisable(this.btnSetSpeed);
                    break;
                case CMD.SETUNLOCK:
                    state = STATE.Lockable;
                    AsyncText(btnStart, "Lock");
                    AsyncColor(btnStart, Color.Orange);
                    //AsyncDisable(this.btnSetSpeed, false);
                    break;
                case CMD.SETPULSEDELAY:
                    AsyncText(toolStripStatusLabel1, "Pulse Delay set.");
                    break;
                case CMD.SETPID:
                    AsyncText(toolStripStatusLabel1, "PID set.");
                    break;
                case CMD.SETFREQ:
                    //TODO: Should SETFREQ -> StartRMTImer ???
                    PersistentLoggerState.ps.data.StartRMTimer();
                    AsyncText(toolStripStatusLabel1, "Target Rotor Frequency set.");
                    break;
                case CMD.TRIGGER:
                    state = STATE.Triggered;

                    if (PersistentLoggerState.ps.data.testingmode != 0)
                    {
                        PrintLn("Sampling triggered", true);
                        //Simulate a trigger in the LAX1664
                        myaio.SimulateTrigger();
                    }

                    break;
                case CMD.SETADC:
                    AsyncText(toolStripStatusLabel1, "ADC set.");
                    break;
            }

            AsyncText(lblState, state.ToString());

            //should we have a queue to time out unsuccessful async tasks
        }


        void SendCommand(CMD cmd)
        {
            string data = "";
            switch ((CMD)cmd)
            {
                case CMD.SETFREQ:
                    data = nudDesireSpeed.Value.ToString();
                    break;
                case CMD.SETPULSEDELAY:
                    //data = nudDesireSpeed.Value.ToString();
                    break;
                case CMD.SETPID:
                    data = nudP.Value.ToString();
                    data += " " + nudI.Value.ToString();
                    data += " " + nudD.Value.ToString();
                    break;
//NEW
                case CMD.SETADC:
                    //TODO: get the value of the clock freq
                    //data = nudP.Value.ToString();
                    data = "1000";
                    break;

            }

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
                    Enums.CMDDecode.TryGetValue(data[1], out CMD ackcmd);
                    ProcessACK(ackcmd);
                    break;
                case DATATYPES.GETPULSEDELAY:
                    PersistentLoggerState.ps.data.pulse_delay = int.Parse(data[1]);
                    AsyncText(lblPulseDelay, data[1]);
                    break;
                case DATATYPES.GETTARGETFREQ:
                    PersistentLoggerState.ps.data.target_speed = float.Parse(data[1]);
                    AsyncNUD(nudDesireSpeed, Decimal.Parse(data[1]));
                    break;
                case DATATYPES.GETROTORFREQ:
                    PersistentLoggerState.ps.data.rotor_speed = float.Parse(data[1]);
                    AsyncText(lblCurrentSpeed, data[1]);
                    break;
                case DATATYPES.GETMINMAXPERIODS:
                    PersistentLoggerState.ps.data.min_period = long.Parse(data[1]);
                    PersistentLoggerState.ps.data.max_period = long.Parse(data[2]);
                    if (PersistentLoggerState.ps.data.IsMMInRange())
                    {
                        AsyncText(lblMinRotorPeriod, data[1]);
                        AsyncText(lblMaxRotorPeriod, data[2]);
                    }
                    else
                    {
                        AsyncText(lblMinRotorPeriod, "-");
                        AsyncText(lblMaxRotorPeriod, "-");
                    }
                    break;
                case DATATYPES.GETPID:
                    PersistentLoggerState.ps.data.p = float.Parse(data[1]);
                    PersistentLoggerState.ps.data.i = float.Parse(data[2]);
                    PersistentLoggerState.ps.data.d = float.Parse(data[3]);
                    AsyncNUD(nudP, Decimal.Parse(data[1]));
                    AsyncNUD(nudI, Decimal.Parse(data[2]));
                    AsyncNUD(nudD, Decimal.Parse(data[3]));
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
                        ProcessACK(CMD.SETUNLOCK);
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

        int timercycle = 0;
        private void some_func(object sender, EventArgs e)
        {
            if (state == STATE.Running)
            {
                //If started but not entered lock cycle yet -> poll to see if lockable
                SendCommand(CMD.GETLOCKABLE);
            }

            SendCommand(CMD.GETROTORFREQ);

            //Log if 4th tick AND Max/Min are meaningful
            if ((timercycle = (++timercycle) % 4) == 0)
            {
                if (PersistentLoggerState.ps.data.IsRMDisabled())
                {
                    //RM (min/max) disable period expired
                    SendCommand(CMD.GETMINMAXPERIODS);
                    SendCommand(CMD.GETPULSEDELAY);
                    SendCommand(CMD.GETPID);
                    //SendCommand(CMD.GETTARGETFREQ); Duh! means can't change!
                    //PersistentLoggerState.ps.data.Write();
                }
            }

        }

        private void nudP_ValueChanged(object sender, EventArgs e)
        {
            SendCommand(CMD.SETPID);
        }

        private void nudI_ValueChanged(object sender, EventArgs e)
        {
            SendCommand(CMD.SETPID);
        }

        private void nudD_ValueChanged(object sender, EventArgs e)
        {
            SendCommand(CMD.SETPID);
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
            //Clear anyway in case no ACK
            ProcessACK(CMD.STOP);
            SendCommand(CMD.STOP);
        }

        private void graphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not implemented yet");
        }

        private void cbxPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.PortName = cbxPort.SelectedItem.ToString();
        }

        public void timerarduino_Tick(object sender, EventArgs e)
        {
            if (state == STATE.Running)
            {
                //If started but not entered lock cycle yet -> poll to see if lockable
                SendCommand(CMD.GETLOCKABLE);
            }

            SendCommand(CMD.GETROTORFREQ);

            //Log if 4th tick AND Max/Min are meaningful
            if ((timercycle = (++timercycle) % 4) == 0)
            {
                if (PersistentLoggerState.ps.data.IsRMDisabled())
                {
                    //RM (min/max) disable period expired
                    SendCommand(CMD.GETMINMAXPERIODS);
                    SendCommand(CMD.GETPULSEDELAY);
                    SendCommand(CMD.GETPID);
                    //SendCommand(CMD.GETTARGETFREQ);
                    //PersistentLoggerState.ps.data.Write();
                }
            }

            //New
            //It is effectively queued to Arduino altho async return
            
            chart1.DataBind();

            cbxInRange.Checked = PersistentLoggerState.ps.data.IsRotorInRange;

            if (PersistentLoggerState.ps.data.IsReadyToSample)
            {
                cbxOK.Checked = true;
                ProcessEvent(EVENT.Lock);
                ProcessEvent(EVENT.Trigger);
            }
        }

        private void UpdateYScale()
        {
            chart1.ChartAreas[0].AxisY.Maximum = (int)(PersistentLoggerState.ps.data.target_speed + PersistentLoggerState.ps.data.graphrange);
            chart1.ChartAreas[0].AxisY.Minimum = (int)(PersistentLoggerState.ps.data.target_speed - PersistentLoggerState.ps.data.graphrange);
        }

    }
}
