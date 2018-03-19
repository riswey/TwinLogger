using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MultiDeviceAIO
{
    public partial class UserCompleteTest : Form
    {
        public UserCompleteTest(string filepath)
        {
            InitializeComponent();

            tbFilename.Text = filepath;

            CheckFilePath(filepath);
        }

        void CheckFilePath(string filepath)
        {
            bool exists = File.Exists(filepath);
            checkBox1.Checked = exists;

            if (exists)
            {
                butOK.Text = "Overwrite";
            }
            else
            {
                butOK.Text = "OK";
            }
        }

        private void tbFilename_TextChanged(object sender, EventArgs e)
        {
            CheckFilePath(this.Text);
        }
    }
}
