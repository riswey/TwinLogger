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

            //TODO make PersistentLoggerState disposable and put in using()
            PersistentLoggerState ps = new PersistentLoggerState();

            bool result = ps.ImportXML(Properties.Settings.Default.processing_settings_current);
            if (!result)
            {
                string msg = "No valid AIOSettings.singleInstance were found. Set to zero.";
            }

            try
            {
                Application.Run(new Main(ps));

            } catch (Exception ex)
            {
                NativeMethods.FailApplication("Game Over", "Problem encountered. App needs to close.\n\n" + ex.Message + "\n" + ex.GetType());
            }
        }
    }
}
