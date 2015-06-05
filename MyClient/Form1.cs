using System;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using MyClient.WcfService;
using Timer = System.Threading.Timer;

namespace MyClient
{
    public partial class Form1 : Form
    {
        private readonly Timer _disposeTimer;
        private static readonly object Padlock = new object();
        private WcfServiceClient _clientProxy;
        private WcfServiceCallback _callback;

        delegate void SetTextCallback(string text);

        public Form1()
        {
            InitializeComponent();
            _disposeTimer = new Timer(DisposeTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void DisposeTimerCallback(object obj)
        {
            lock (Padlock)
            {
                CloseServiceClient();
                _disposeTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked) GetValueSynchronously();

            else if (radioButton2.Checked) GetValueAsynchronously();
        }

        private void GetValueAsynchronously()
        {
            AddInformation("Async: Start");

            AttemptCreateServiceClient();

            try
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    AddInformation("Async: Server call");
                    _clientProxy.GetValueAsynchronously(Thread.CurrentThread.ManagedThreadId);
                });
            }
            catch (Exception ex)
            {
                AddInformation("Async: communication failure" + Environment.NewLine + ex.Message);
            }
        }
        
        private void callback_GetValueAsynchronouslyEvent(string message)
        {
            AddInformation("Async: message - " + message);
            _disposeTimer.Change(15000, Timeout.Infinite);
        }
        
        private void GetValueSynchronously()
        {
            AddInformation("GetValueSynchronously: Start");

            AttemptCreateServiceClient();

            try
            {
                AddInformation("GetValueSynchronously: Server call");
                var message = _clientProxy.GetValueSynchronously(Thread.CurrentThread.ManagedThreadId);
                AddInformation("GetValueSynchronously: message - " + message);
            }
            catch (Exception ex)
            {
                AddInformation("GetValueSynchronously: communication failure" + Environment.NewLine + ex.Message);
            }
            finally
            {
                _disposeTimer.Change(15000, Timeout.Infinite);
            }
        }

        private void AttemptCreateServiceClient()
        {
            if (_clientProxy != null &&
                _clientProxy.State == CommunicationState.Opened)
            {
                _disposeTimer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            _callback = new WcfServiceCallback();
            _callback.GetValueAsynchronouslyEvent += callback_GetValueAsynchronouslyEvent;

            var instanceContext = new InstanceContext(_callback);
            var endpoint = new EndpointAddress("net.tcp://localhost:12906/WcfCallbacks/WcfService/");
            var binding = new NetTcpBinding
            {
                Security = {Mode = SecurityMode.None},
                SendTimeout = new TimeSpan(2, 0, 0),
                ReceiveTimeout = new TimeSpan(2, 0, 0),
                OpenTimeout = new TimeSpan(0, 1, 0),
                CloseTimeout = new TimeSpan(0, 1, 0),
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas =
                {
                    MaxArrayLength = 2147483647,
                    MaxStringContentLength = 2147483647,
                    MaxBytesPerRead = 2147483647,
                    MaxNameTableCharCount = 2147483647,
                    MaxDepth = 32
                }
            };

            _clientProxy = new WcfServiceClient(instanceContext, binding, endpoint);

            try
            {
                _clientProxy.Open();
                AddInformation("ClientProxy: Open");
            }
            catch (Exception e)
            {
                AddInformation(e.Message);
            }
        }

        private void CloseServiceClient()
        {
            var success = false;
            try
            {
                _clientProxy.Close();
                success = true;
            }
            catch (CommunicationException ce)
            {
                _clientProxy.Abort();
            }
            catch (TimeoutException te)
            {
                _clientProxy.Abort();
            }
            finally
            {
                if (!success)
                    _clientProxy.Abort();

                AddInformation("ClientProxy: Closed");
            }
        }

        // This method is passed in to the SetTextCallBack delegate 
        // to set the Text property of InformationTextBox. 
        private void SetText(string text)
        {
            //txtInformation.Text = text;
            //txtInformation.SelectionStart = txtInformation.TextLength;
            //txtInformation.ScrollToCaret();
            txtInformation.AppendText(text);
        }

        private void AddInformation(string message)
        {
            var text = DateTime.Now + "\t" + Thread.CurrentThread.ManagedThreadId + "\t" + message + Environment.NewLine;

            if (txtInformation.InvokeRequired)
            {
                // It's on a different thread, so use Invoke.
                var d = new SetTextCallback(SetText);
                txtInformation.Invoke(d, new object[] { text });
            }
            else
            {
                txtInformation.AppendText(text);
            }
        }
    }
}
