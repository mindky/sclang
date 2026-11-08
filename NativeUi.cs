using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScLang
{
    internal static class NativeUi
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void DarkTitleBar(Form form)
        {
            if (form == null) return;
            try
            {
                IntPtr handle = form.Handle;
                int value = 1;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            }
            catch
            {
            }
        }
    }
}
