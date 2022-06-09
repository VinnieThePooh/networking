using Sockets.Common.Models;
namespace Networking.MessagingModels.Services;

public interface IItemQuoteDecoder
{
    ItemQuote Decode(Stream source);
    ItemQuote Decode(byte[] packet);
}
