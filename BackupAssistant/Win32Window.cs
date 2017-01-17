using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace BackupAssistant
{
    public class Win32Window : System.Windows.Forms.IWin32Window
    {
        IntPtr handle;

        public Win32Window(Window window)
        {
            handle = new WindowInteropHelper(window).Handle;
        }

        public IntPtr Handle
        {
            get { return handle; }
        }
    }
}
