using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MultiDeviceAIO
{
    public partial class UserSettings : Form
    {
        LoggerState settings;

        public UserSettings(LoggerState settings)
        {
            this.settings = settings;

            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> dict = LoggerState.MergeDictionary(settings);

            string text = String.Join("}\n{", dict.Keys.ToArray());

            MessageBox.Show("{" + text + "}");
        }
    }
}
