using System.ServiceModel;

namespace WcfCallbacks
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWcfService" in both code and config file together.
    [ServiceContract(CallbackContract = typeof(IWcfServiceCallback))]
    public interface IWcfService
    {
        [OperationContract]
        string GetValueSynchronously(int returnId);

        [OperationContract]
        void GetValueAsynchronously(int returnid);
    }
}
