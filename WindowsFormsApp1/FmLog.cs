using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;

namespace MultiDeviceAIO
{
    public partial class FmLog : Form
    {
        string text = "";

        public FmLog()
        {
            InitializeComponent();
        }

        public void PrintLn(object msg, bool speak = false, int linebreak = 1)
        {
            switch (linebreak)
            {
                case -1:
                    string[] txt = text.Split('\n');
                    text = String.Join("\n", txt, 0, txt.Length - 2) + "\n" + msg.ToString() + "\r\n";
                    break;
                case 0:
                    text += msg.ToString();
                    break;
                case 1:
                    text += msg.ToString() + "\r\n";
                    break;
            }

            textBox1.Text = text;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();

            if (speak) SayMessage(msg.ToString());
        }

        public void SaveLogFile()
        {
            File.WriteAllText(PersistentLoggerState.ps.data.testpath + @"\log.txt", text);
        }

        void SayMessage(string msg)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => SayMessage(msg)));
                return;
            }

            // Initialize a new instance of the SpeechSynthesizer.  
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                synth.SetOutputToDefaultAudioDevice();
                synth.Volume = 100;  // (0 - 100)
                synth.Speak(msg);
            }
        }

        //Can't close
        private void FmLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;
        }
    }
}
