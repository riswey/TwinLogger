﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Threading.Tasks;

/*
 * Driven by the client
 * 
 * When they write to me, I plan a response and set the buffer
 * 
 */

namespace MotorController
{
    public class TestSerialPort
    {
        struct State
        {
            public int running; //0=stopped,1=running,2=lockable,3=triggered
            public float p;
            public float i;
            public float d;
            public bool locked;
            public float target_f;
            public long min;
            public long max;
        };

        float NOISELEVEL = 5.0f;

        int _PulseDelay = 0;
        public int PulseDelay
        {
            get
            {
                if (!state.locked) _PulseDelay = r.Next(0, 100);
                return _PulseDelay;
            }
        }

        float t = 0;
        float _roto_freq = 0;
        public int RotorFreq
        {
            get
            {
                t += 0.1f;
                float expected = (int)(state.target_f + 120 * (Math.Cos(t) / (t + 1)) + r.NextDouble());
                return (int)(_roto_freq += (expected - _roto_freq) / 10 * NOISELEVEL);
            }
        }

        State state = new State
        {
            running = 0,
            p = 0,
            i = 0,
            d = 0,
            locked = false,
            target_f = 0,
            min = (long)1E7,
            max = 0
        };

        Random r = new Random();

        private MultiDeviceAIO.FmControlPanel parentfm;

        private string buffer = "";

        public int BaudRate { get; set; }
        public string PortName { get; set; }

        public TestSerialPort(MultiDeviceAIO.FmControlPanel parentfm)
        {
            this.parentfm = parentfm;
        }

        public bool IsOpen { get { return true; } }
        public void Open() { }
        public void DiscardInBuffer() { }
        public void Close() { }

        private void Send(string buf)
        {
            buffer = buf + "\n";
            parentfm.serialPort1_DataReceived(this, null);
        }


        int maxmin = 0;
        public void Write(string packet)
        {
            //What CMD did I just get asked to do
            //sim Dave's API
            string[] data = packet.Split(' ');
            Enums.CMDDecode.TryGetValue(data[0], out CMD cmd);

            switch (cmd)
            {
                case CMD.START:
                    state.p = 0;
                    state.i = 0;
                    state.d = 0;
                    state.locked = false;
                    state.running = 1;
                    //TODO: This cannot be done in 3.5 Find another way
                    Task task = Task.Delay(5000).ContinueWith(t => state.running = 2);
                    SendACK(cmd);
                    break;
                case CMD.STOP:
                    state.running = 0;
                    SendACK(cmd);
                    break;
                case CMD.SETLOCK:
                    //Can't lock until state = 2
                    if (state.running == 2)
                        state.locked = true;
                    SendACK(cmd);
                    break;
                case CMD.SETUNLOCK:
                    if (state.locked == true)
                        state.locked = false;
                    SendACK(cmd);
                    break;
                case CMD.SETPULSEDELAY:
                    _PulseDelay = int.Parse(data[1]);
                    SendACK(cmd);
                    break;
                case CMD.SETPID:
                    state.p = float.Parse(data[1]);
                    state.i = float.Parse(data[2]);
                    state.d = float.Parse(data[3]);
                    SendACK(cmd);
                    break;
                case CMD.SETFREQ:
                    state.target_f = int.Parse(data[1]);
                    SendACK(cmd);
                    break;

                // NO ACK
                case CMD.GETTARGETFREQ:
                    Send("FW " + state.target_f);
                    break;
                case CMD.GETROTORFREQ:
                    Send("CF " + RotorFreq);
                    break;
                case CMD.GETPULSEDELAY:
                    Send("PW " + PulseDelay);
                    break;
                case CMD.GETPID:
                    Send(String.Format("PID {0} {1} {2}", state.p, state.i, state.d));
                    break;
                case CMD.GETMINMAXPERIODS:
                    if (maxmin++ > 5)
                    {
                        state.min = r.Next(0, 10);
                        state.max = r.Next(0, 10);
                    }

                    Send(String.Format("MM {0} {1}", state.min, state.max));

                    break;
                case CMD.GETLOCKABLE:
                    //If state.running = 2 then lockable. Answer true
                    Send(String.Format("TL {0}", state.running == 2));
                    break;

                //NEW

                case CMD.TRIGGER:
                    SendACK(cmd);
                    break;
                case CMD.SETADC:
                    //Do something with int.Parse(data[1]);
                    SendACK(cmd);
                    break;
                case CMD.GETADC:
                    Send(String.Format("RA {0}", 2001));
                    break;

                default:
                    parentfm.Msg("Unknown packet: " + cmd);
                    break;

            }
            //SendLine("CF " + r.Next(0,100).ToString() );

        }
        //NEW
        void SendACK(CMD cmd)
        {
            Enums.CMDEncode.TryGetValue(cmd, out string str);
            Send("ACK " + str);
        }
        //
        public string ReadLine()
        {
            return buffer;
        }

        public string[] GetPortNames()
        {
            return new string[] { "COM6" };
        }
    }
}
