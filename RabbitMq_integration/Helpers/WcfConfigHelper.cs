using System.ServiceModel;
using System.ServiceModel.Channels;

namespace RabbitMq_integration.Helpers;

public class WcfConfigHelper
{
    /// <summary>
    /// Tries to set ProxyAddress on the <see cref="Binding"/>, or throw an Exception if no such property can be found on the binding type.
    /// </summary>
    /// <param name="proxyAddress"></param>
    /// <param name="endpointBinding"></param>
    public static void SetProxyAddressInBinding(string proxyAddress, Binding endpointBinding)
    {
        switch (endpointBinding)
        {
            case BasicHttpBinding httpBinding:
                httpBinding.ProxyAddress = new Uri(proxyAddress);
                httpBinding.UseDefaultWebProxy = false;
                break;
            case CustomBinding customBinding:
            {
                var httpsBindingElement = customBinding.Elements.OfType<HttpsTransportBindingElement>().SingleOrDefault();

                if (httpsBindingElement != null)
                {
                    httpsBindingElement.ProxyAddress = new Uri(proxyAddress);
                    httpsBindingElement.UseDefaultWebProxy = false;
                }
                else
                {
                    throw new Exception("CustomBinding has no BindingElement with ProxyAddress.");
                }

                break;
            }
            default:
                throw new Exception($"Binding of type {endpointBinding.GetType()} has no ProxyAddress.");
        }
    }
}