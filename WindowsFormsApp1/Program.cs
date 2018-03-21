using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MultiDeviceAIO
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new Main());
            } catch (Exception ex)
            {
                Sys.FailApplication("Game Over", "Problem encountered. App needs to close.\n\n" + ex.Message + "\n" + ex.GetType());
            }
        }
    }
}
