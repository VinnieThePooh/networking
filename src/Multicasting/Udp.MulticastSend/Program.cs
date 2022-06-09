using System.Net;
using System.Net.Sockets;
using Networking.MessagingModels.DataEncoding;
using Sockets.Common.Infrastructure;
using Sockets.Common.Models;

if (args.Length != 2 && args.Length != 3)
    throw new ArgumentException("Parameter(s): <Destination> <Port> [<encoding>]");

var server = args[0];
int destPort = int.Parse(args[1]);

var quote = new ItemQuote(1234567890987654L, "5mm Super Widgets", 1000, 12999, true, false);

var client = new UdpClient();
var encoder = args.Length == 3 ? new ItemQuoteEncoderText(args[2]) : new ItemQuoteEncoderText();

byte[] codedQuote = encoder.Encode(quote);

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) => cts.Cancel();

var destinationEndpoint = new IPEndPoint(IPAddress.Parse(server), destPort);
DummyLogger.Log($"UdpClient is sending data to {client.Client.ProtocolType}:{client.Client.RemoteEndPoint}");

try
{
    while (!cts.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(300), cts.Token);
        await client.SendAsync(codedQuote, destinationEndpoint, cts.Token);
        DummyLogger.Log($" Sent packet of ItemQuote data ({codedQuote.Length} bytes)");
        cts.Token.ThrowIfCancellationRequested();
    }
}
catch (OperationCanceledException e)
{
    DummyLogger.Log("Sending cancelled");
}
finally
{
    client.Close();
}