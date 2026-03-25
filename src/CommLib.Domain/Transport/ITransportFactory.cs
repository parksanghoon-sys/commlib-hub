using CommLib.Domain.Configuration;

namespace CommLib.Domain.Transport;

public interface ITransportFactory
{
    ITransport Create(TransportOptions options);
}
