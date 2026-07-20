using System.ServiceModel;

namespace WcfClient
{
    public class Client
    {
        public object Factory;

        public void Connect()
        {
            Factory = typeof(System.ServiceModel.ChannelFactory);
        }
    }
}
