using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Unmanaged.Headers;

namespace Unmanaged.Libraries
{
    sealed class user32
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern Boolean ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll")]
        public static extern Boolean CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
           Winuser.WindowStylesEx dwExStyle,
           [MarshalAs(UnmanagedType.LPStr)] 
           String lpClassName,
           [MarshalAs(UnmanagedType.LPStr)] String lpWindowName, 
           Winuser.WindowStyles dwStyle, Int32 x, Int32 y, Int32 nWidth, Int32 nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
           Winuser.WindowStylesEx dwExStyle,
           IntPtr lpClassName,
           [MarshalAs(UnmanagedType.LPStr)] String lpWindowName,
           Winuser.WindowStyles dwStyle, Int32 x, Int32 y, Int32 nWidth, Int32 nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DefWindowProcW(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] String lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DispatchMessage(ref Winuser.tagMSG lpMsg);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 EnumClipboardFormats(UInt32 format);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt16 GetAsyncKeyState(UInt32 vKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt16 GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetClipboardData(UInt32 uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean GetMessage(ref Winuser.tagMSG lpMsg, IntPtr hWnd, UInt32 wMsgFilterMin, UInt32 wMsgFilterMax);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 GetClipboardSequenceNumber();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, UInt32 nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean PostMessage(IntPtr hWnd, UInt32 Msg, UInt32 wParam, UInt32 lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt16 RegisterClassEx(ref Winuser.WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] String lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean TranslateMessage(ref Winuser.tagMSG lpMsg);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean UnregisterClass(String lpClassName, IntPtr hInstance);
    }
}