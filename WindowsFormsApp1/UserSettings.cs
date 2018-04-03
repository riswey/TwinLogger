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
        public UserSettings(SettingData settings)
        {
            InitializeComponent();

            Dictionary<string, string> dict = SettingData.MergeDictionary(settings);

            label2.Text = String.Join(",", dict.Keys.ToArray());

        }
    }
}
