using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using NLog;

namespace WcfCallbacks
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WcfService" in both code and config file together.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class WcfService : IWcfService
    {
        private Timer _threadTimer;
        private static readonly object Padlock = new object();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Dictionary<Guid, IWcfServiceCallback> _callbacks;
        public Dictionary<Guid, IWcfServiceCallback> Callbacks
        {
            get { return _callbacks ?? (_callbacks = new Dictionary<Guid, IWcfServiceCallback>()); }
            set { _callbacks = value; }
        }

        public WcfService()
        {
            _threadTimer = new Timer(ThreadTimerCallback, null, 15000, Timeout.Infinite);
        }

        private void ThreadTimerCallback(object obj)
        {
            AddTraceLogging("ThreadTimerCallback");
            lock (Padlock)
                AttemptCallbackCleanup();

            _threadTimer.Change(15000, Timeout.Infinite);
        }

        private IWcfServiceCallback GetCallbackObject()
        {
            var callback = OperationContext.Current.GetCallbackChannel<IWcfServiceCallback>();

            if (Callbacks.ContainsValue(callback))
                return callback;

            lock (Padlock)
            {
                var key = Guid.NewGuid();
                Callbacks.Add(key, callback);
                AddTraceLogging("Callback: ADD[" + key + "]");
            }

            return callback;
        }

        private void AttemptCallbackCleanup()
        {
            if (Callbacks == null || !Callbacks.Any())
                return;
            
            var removeKeys = new List<Guid>();
            foreach (var callback in Callbacks)
            {
                var thisCallback = (ICommunicationObject)callback.Value;

                if (thisCallback.State == CommunicationState.Closed ||
                    thisCallback.State == CommunicationState.Closing ||
                    thisCallback.State == CommunicationState.Faulted)
                    removeKeys.Add(callback.Key);
            }

            foreach (var key in removeKeys)
            {
                Callbacks.Remove(key);
                AddTraceLogging("Callback: DROP[" + key + "]");
            }
        }

        public void GetValueAsynchronously(int returnId)
        {
            AddTraceLogging(string.Format("GetValueAsynchronously"));

            var callback = GetCallbackObject();
            //manually forcing the async operation to take a few seconds longer
            Thread.Sleep(5000);
            callback.GetValueAsynchronouslyCallback("Async Callback");
        }

        public string GetValueSynchronously(int returnId)
        {
            AddTraceLogging(string.Format("GetValueSynchronously"));

            var callback = GetCallbackObject();

            return "Sync Return";
        }

        private void AddTraceLogging(string message)
        {
            var text = DateTime.Now + "\t" + Thread.CurrentThread.ManagedThreadId + "\t" + message;
#if DEBUG
            Console.WriteLine(text);
#endif
            Logger.Trace(text);
        }
    }
}
