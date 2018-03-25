using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//Dllimport
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MultiDeviceAIO
{
    class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hModule);

        public static bool CheckLibrary(string fileName)
        {
            IntPtr hinstLib = LoadLibrary(fileName);
            FreeLibrary(hinstLib);
            return hinstLib != IntPtr.Zero;
        }

        public static void FailApplication(string title, string message)
        {
            MessageBox.Show
            (
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            Environment.Exit(0);
        }

    }
}
