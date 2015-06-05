using System;
using System.ServiceModel;
using System.Threading;
using MyClient.WcfService;

namespace MyClient
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class WcfServiceCallback : IWcfServiceCallback
    {
        public delegate void GetValueAsynchronouslyEventHandler(string message);
        public event GetValueAsynchronouslyEventHandler GetValueAsynchronouslyEvent;

        public void GetValueAsynchronouslyCallback(string message)
        {
            if (GetValueAsynchronouslyEvent == null)
                return;

            GetValueAsynchronouslyEvent(message);

            /* THIS IS AN ALTERNATIVE WAY TO CLOSE THE CHANNEL VIA CALLBACK
             * 
             * var currentChannel = OperationContext.Current.Channel;
             * //sleep before close so the service has a chance to catch up
             * ThreadPool.QueueUserWorkItem(o =>
             * {
             * Thread.Sleep(15000);
             * CloseChannel(currentChannel);
             * });
            */
        }

        private static void CloseChannel<T>(T channel)
        {
            var clientChannel = (IClientChannel)channel;
            var success = false;
            try
            {
                clientChannel.Close();
                success = true;
            }
            catch (CommunicationException ce)
            {
                clientChannel.Abort();
            }
            catch (TimeoutException te)
            {
                clientChannel.Abort();
            }
            finally
            {
                if (!success)
                    clientChannel.Abort();
            }
        }
    }
}
