using System;
using System.ServiceModel;
using System.Threading;
using NLog;

namespace WcfCallbacks
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static ServiceHost _host;
        private static void Main(string[] args)
        {
            Start();
            while (true)
            {
                var readLine = Console.ReadLine();
                if (readLine != null)
                {
                    var cmd = readLine.ToLower();

                    if (string.Equals("exit", cmd))
                    {
                        break;
                    }

                    if (string.Equals("restart", cmd))
                    {
                        Stop();
                        Start();
                        continue;
                    }
                }
                Console.WriteLine(@"EXIT" + "\t" + "Exits the application.");
                Console.WriteLine(@"RESTART" + "\t" + "Stops and restarts the application.");
            }
            Stop();
        }

        public static void Start()
        {
            AddTraceLogging("WcfService Starting...");
            try
            {
                var address = new Uri(@"net.tcp://localhost:12906/WcfCallbacks/WcfService");
                _host = new ServiceHost(typeof(WcfService), address);
                _host.Open();
            }
            catch (Exception ex)
            {
                AddTraceLogging(ex.ToString());
                Stop();
                return;
            }

            AddTraceLogging("WcfService Running...");
        }

        public static void Stop()
        {
            AddTraceLogging("WcfService Stopping...");
            try
            {
                _host.Close();
            }
            catch
            {
                //Swallow when service is shutting down                    
            }
        }

        private static void AddTraceLogging(string message)
        {
            var text = DateTime.Now + "\t" + Thread.CurrentThread.ManagedThreadId + "\t" + message;
#if DEBUG
            Console.WriteLine(text);
#endif
            Logger.Trace(text);
        }
    }
}
