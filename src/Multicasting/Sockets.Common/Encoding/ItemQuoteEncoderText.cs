using Sockets.Common.Models;
using System.Text;
using Networking.MessagingModels.Services;
using Networking.MessagingModels.Constants;

namespace Networking.MessagingModels.DataEncoding;

public class ItemQuoteEncoderText : IItemQuoteEncoder
{
    private readonly StringBuilder stringBuilder = new StringBuilder();

    public ItemQuoteEncoderText():this(ItemQuoteTextConstants.DEFAULT_CHAR_ENC)
    {
    }

    public ItemQuoteEncoderText(string encodingDesc)
    {
        Encoding = Encoding.GetEncoding(encodingDesc);
    }

    public Encoding Encoding { get; private set; }

    public byte[] Encode(ItemQuote item)
    {
        stringBuilder.Clear();
        stringBuilder.Append($"{item.ItemNumber} ");

        if (item.Description.IndexOf("\n", StringComparison.Ordinal) != -1)
            throw new IOException("Invalid description (contins newline)");

        stringBuilder.Append($"{item.Description}\n");
        stringBuilder.Append($"{item.Quantity} ");
        stringBuilder.Append($"{item.UnitPrice} ");

        if (item.Discounted)
            stringBuilder.Append("d");
        if (item.InStock)
            stringBuilder.Append("s");

        stringBuilder.Append("\n");

        var result = stringBuilder.ToString();

        if (result.Length > ItemQuoteTextConstants.MAX_WIRE_LENGTH)
            throw new IOException("Encoded length too long");

        return Encoding.GetBytes(result);
    }
}
