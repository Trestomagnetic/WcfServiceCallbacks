using System.ServiceModel;

namespace WcfCallbacks
{
    [ServiceContract(Name = "IWcfServiceCallback")]
    public interface IWcfServiceCallback
    {
        [OperationContract]
        void GetValueAsynchronouslyCallback(string message);
    }
}
