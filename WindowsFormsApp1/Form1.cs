using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaioCs;

namespace WindowsFormsApp1
{

    public partial class Form1 : Form
    {
        Caio aio = new Caio();
        short id;               //stores open device id

        void log(String msg, int flag = 0)
        {
            //write to dump
            txtLog.Text += msg + "\r\n";

            //alert user
            if ((flag & 1) != 0)
            {
                lblStatus.Text = msg;
            }

            if ((flag & 2) != 0)
            {
                MessageBox.Show(msg);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            String device_name = txtDevice.Text;

            log("Initialising..." + device_name);

            long ret = aio.Init( device_name, out id);

            log("Init return code: " + ret);

            if (ret != 0)
            {
                log("Initialise failed!", 3);
            } else {
                log("Initialise SUCCESS!!!", 3);
            }

            log("------------------------------");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            aio.Exit(id);
        }

    }
}
