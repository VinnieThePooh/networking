using Sockets.Common.Models;

namespace Networking.MessagingModels.Services;

public interface IItemQuoteEncoder
{
    byte[] Encode(ItemQuote item);
}
