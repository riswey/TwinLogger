using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

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
            PersistentLoggerState.ps = new PersistentLoggerState();

            if (File.Exists("settings.xml"))
            {
                PersistentLoggerState.ps.Load("settings.xml");
            }
            else
            {
                bool result = PersistentLoggerState.ps.ImportXML(Properties.Settings.Default.processing_settings_current);
                if (!result)
                {
                    string msg = "No valid AIOSettings.singleInstance were found. Set to zero.";
                }
            }

#if TESTING
            Application.Run(new Main());
#else
            try
            {
                Application.Run(new Main());
            }
            catch (Exception ex)
            {
                NativeMethods.FailApplication("Game Over", "Problem encountered. App needs to close.\n\n" + ex.Message + "\n" + ex.GetType());
            }
#endif
        }
    }
}
