using Networking.MessagingModels.Constants;
using Sockets.Common.Models;
using Networking.MessagingModels.Services;
using System.Text;
using Sockets.Common.Framing;

namespace Sockets.Common.ENcoding;

public class ItemQuoteDecoderText: IItemQuoteDecoder
{
    public ItemQuoteDecoderText():this(ItemQuoteTextConstants.DEFAULT_CHAR_ENC)
    {
    }

    public ItemQuoteDecoderText(string encodingDesc)
    {
        Encoding = Encoding.GetEncoding(encodingDesc);
    }

    public Encoding Encoding { get; init; }

    public ItemQuote Decode(Stream source)
    {
        string itemN, description, quantity, price, flags;

        byte[] space = Encoding.GetBytes(" ");
        byte[] newLine = Encoding.GetBytes("\n");

        itemN = Encoding.GetString(Framer.NextToken(source, space));
        description = Encoding.GetString(Framer.NextToken(source, newLine));
        quantity = Encoding.GetString(Framer.NextToken(source, space));
        price = Encoding.GetString(Framer.NextToken(source, space));
        flags = Encoding.GetString(Framer.NextToken(source, newLine));

        return new ItemQuote(
            long.Parse(itemN),
            description,
            int.Parse(price),
            int.Parse(quantity),
            flags.IndexOf('d') != -1,
            flags.IndexOf('s') != -1);
    }

    public ItemQuote Decode(byte[] packet)
    {
        var payload = new MemoryStream(packet, 0, packet.Length, false);
        return Decode(payload);
    }
}
