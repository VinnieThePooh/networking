using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProcessInfo.Client.Settings;

namespace ProcessInfo.Client
{
    public class ProcessInfoClient: IDisposable
    {
        public NetworkingSettings Settings { get; }

        private CancellationTokenSource cts;

        private Socket clientSocket;

        public ProcessInfoClient(NetworkingSettings settings)
        {
            Settings = settings;
        }

        public async Task StartSendingInfo()
        {
            cts = new CancellationTokenSource();

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(Settings.ClientAddress), Settings.ClientPort);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                clientSocket.Bind(tcpEndPoint);
                var remoteEndpoint = new IPEndPoint(IPAddress.Parse(Settings.ServerAddress), Settings.ServerPort);
                await clientSocket.ConnectAsync(remoteEndpoint);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not connect to {Settings.ClientAddress}:{Settings.ServerPort}:");
                Console.WriteLine(e.ToString());
            }

            while (!cts.IsCancellationRequested)
            {                
                Process[] processes = Process.GetProcesses();
                Console.WriteLine($"[{DateTime.Now}]: {processes.Length} processes detected on a client machine");
                string message;
                int i = 1;
                foreach (var process in processes)
                {
                    message = $"Process {i}) {process.ProcessName}  {process.BasePriority}";
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    var length = IPAddress.HostToNetworkOrder(messageBytes.Length);
                    try
                    {
                        var lengthBytes = BitConverter.GetBytes(length);

                        await clientSocket.SendAsync(lengthBytes, SocketFlags.None, cts.Token);
                        await clientSocket.SendAsync(messageBytes, SocketFlags.None, cts.Token);
                        Console.WriteLine($"[{DateTime.Now}]: {i++}.Sent data to server (4 + {messageBytes.Length} bytes): {message}");
                    }
                    catch (OperationCanceledException e)
                    {
                        Console.WriteLine("Cancelled sending data");
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine(se.Message);
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not connect to {Settings.ClientAddress}:{Settings.ServerPort}:");
                        Console.WriteLine(e.ToString());
                    }                                        
                }
                await Task.Delay(TimeSpan.FromSeconds(Settings.SendInterval));
            }
        }

        public Task StopSendingInfo()
        {
            var completed = Task.CompletedTask;

            if (cts.IsCancellationRequested)
                return completed;

            cts.Cancel();
            Dispose();
            return completed;
        }

        public void Dispose()
        {
            cts?.Dispose();
            clientSocket?.Dispose();
        }
    }
}
