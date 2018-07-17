using System;
using System.Runtime.InteropServices;

namespace Unmanaged.Libraries
{
    sealed class user32
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean AddClipboardFormatListener(
            IntPtr hwnd
        );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetClipboardViewer(
            IntPtr hWndNewViewer
        );
    }
}