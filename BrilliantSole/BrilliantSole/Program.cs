using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrilliantSole
{
    /// <summary>
    /// Application entry point for thhe console application.
    /// </summary>
    class Program
    {
        #region exit handing
        static bool exitSystem = false;
        // https://stackoverflow.com/questions/1119841/net-console-application-exit-event
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        private static EventHandler handler;
        #endregion

        static void Main(string[] args)
        {
            handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(handler, true);

            ConnectionManager.Instance.Start();
            string baseAddress = "http://localhost:9000/";
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                // Test api
                HttpClient client = new HttpClient();
                var response = client.GetAsync(baseAddress + "api/devices").Result;
                Console.WriteLine(response);
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                //hold the console so it doesn’t run off the end
                while (!exitSystem)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");

            //do your cleanup here
            ConnectionManager.Instance.Disconnect();

            Console.WriteLine("Cleanup complete. Press a key to exit.");
            Console.ReadLine();

            //allow main to run off
            exitSystem = true;

            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);

            return true;
        }
    }
}
