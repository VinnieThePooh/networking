using System.Net;
using System.Net.Sockets;
using Networking.MessagingModels.Constants;
using Sockets.Common.ENcoding;
using Sockets.Common.Infrastructure;

if (args.Length != 2)
    throw new ArgumentException("Parameters(s): <Multicast Addr>  <Port>");

var address = IPAddress.Parse(args[0]);

if (!IsValidMultiicast(args[0]))
    throw new ArgumentException("Valid MC address: 224.0.0.0 - 239.255.255.155");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) => cts.Cancel();

int port = int.Parse(args[1]);
var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

var endpoint = new IPEndPoint(IPAddress.Any, port);
sock.Bind(endpoint);
Console.WriteLine($"Udp socket is bound to endpoint: {sock.ProtocolType}:{sock.LocalEndPoint}");

//multicast membership
sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(address, IPAddress.Any));

var receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

byte[] packet = new byte[ItemQuoteTextConstants.MAX_WIRE_LENGTH];
var decoder = new ItemQuoteDecoderText();

try
{
    while (!cts.IsCancellationRequested)
    {
        var result = await sock.ReceiveFromAsync(packet, SocketFlags.None, receiveEndpoint);
        var quote = decoder.Decode(packet);
        DummyLogger.Log($"Group received a quote packet: ({result.ReceivedBytes} bytes)");
        Console.WriteLine(quote);
        cts.Token.ThrowIfCancellationRequested();
    }
}
catch (OperationCanceledException e)
{
    DummyLogger.Log("Receiving multicast cancelled.");
}
finally
{
    //drop membership
    sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(address, IPAddress.Any));
    sock.Close();
}


static bool IsValidMultiicast(string address)
{
    try
    {
        int oct1 = int.Parse(address.Split(new char[] { '.' })[0]);

        return
            oct1 >= 224 && oct1 <= 239;
    }
    catch (Exception)
    {
        return false;
    }
}