using System;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    sealed class Clipboard : Base
    {
        private static IntPtr hWndClipboard;
        private static IntPtr hWndViewer;
        private static IntPtr hClipboardData;

        public Clipboard()
        {
        }

        public void Monitor()
        {
            IntPtr hInstance = kernel32.GetModuleHandle(String.Empty);

            Winuser.WNDCLASSEX lpwcx = Winuser.WNDCLASSEX.Build();
            //lpwcx.lpfnWndProc = ;
            lpwcx.lpszClassName = "Monitor";

            UInt16 lpClassName = user32.RegisterClassEx(ref lpwcx); 
            
            hWndClipboard = user32.CreateWindowEx(Winuser.WindowStylesEx.WS_EX_RIGHTSCROLLBAR, Convert.ToString(lpClassName), "Monitor", 0, 0, 0, 0, 0, Winuser.HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);
            if (IntPtr.Zero == hWndClipboard)
            {
                WriteOutputBad("CreateWindowEx Failed");
            }
            
            kernel32.SetConsoleCtrlHandler(new kernel32.HandlerRoutine(HandlerRoutine), true);
            hWndViewer = user32.SetClipboardViewer(hWndClipboard);
            Winuser.tagMSG lpMsg= new Winuser.tagMSG();
            while (user32.GetMessage(ref lpMsg, hWndViewer, Winuser.WM_QUIT, Winuser.WM_CHANGECBCHAIN))
            {
                user32.TranslateMessage(ref lpMsg);
                user32.DispatchMessage(ref lpMsg);
            }
            if (!user32.ChangeClipboardChain(hWndClipboard, hWndViewer))
            {
                WriteOutputBad("ChangeClipboardChain Failed");
            }

            kernel32.SetConsoleCtrlHandler(new kernel32.HandlerRoutine(HandlerRoutine), false);

            if (!user32.DestroyWindow(hWndClipboard))
            {
                WriteOutputBad("DestroyWindow Failed");
            }
            if (!user32.UnregisterClass(Convert.ToString(lpClassName), hInstance))
            {
                WriteOutputBad("UnregisterClass Failed");
            }
        }

        public static Boolean HandlerRoutine(Wincon.CtrlType dwCtrlType)
        {
            if (IntPtr.Zero != hClipboardData)
            {
                kernel32.LocalFree(hClipboardData);
            }

            if (IntPtr.Zero != hWndClipboard)
            {
                user32.PostMessage(hWndClipboard, Winuser.WM_QUIT, 0x00000080, 0);
            }

            switch (dwCtrlType)
            {
                case Wincon.CtrlType.CTRL_C_EVENT:
                    return true;
                case Wincon.CtrlType.CTRL_BREAK_EVENT:
                    return true;
                case Wincon.CtrlType.CTRL_CLOSE_EVENT:
                    return false;
                case Wincon.CtrlType.CTRL_LOGOFF_EVENT:
                    return false;
                case Wincon.CtrlType.CTRL_SHUTDOWN_EVENT:
                    return false;
                default:
                    return false;
            }
        }

        public static IntPtr FnWndProc(IntPtr hwnd, UInt32 uMsg, UInt32 wParam, UInt32 lParam)
        {
            IntPtr result = new IntPtr();

            return result;
        }
    }
}