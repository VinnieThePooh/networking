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
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ulong totalSent;

            try
            {                
                var remoteEndpoint = new IPEndPoint(IPAddress.Parse(Settings.ServerAddress), Settings.ServerPort);
                await clientSocket.ConnectAsync(remoteEndpoint);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not connect to {Settings.ServerAddress}:{Settings.ServerPort}:");
                Console.WriteLine(e.ToString());
            }

            while (!cts.IsCancellationRequested)
            {
                totalSent = 0;
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
                        totalSent += (ulong)(4 + messageBytes.Length);
                        Console.WriteLine($"[{DateTime.Now}]: {i++}.Sent data to server (4 + {messageBytes.Length} bytes): {message}");
                    }
                    catch (OperationCanceledException e)
                    {
                        Console.WriteLine("Cancelled sending data");
                        cts.Cancel();
                        break;
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine(se.Message);
                        cts.Cancel();
                        break;
                    }
                    catch (Exception e)
                    {                                             
                        Console.WriteLine(e.ToString());
                        cts.Cancel();
                        break;
                    }                    
                }

                var endMessage = await SendEndMessage(totalSent, processes.Length);
                Console.WriteLine(endMessage);
                Console.WriteLine(new string('-',endMessage.Length) + "\n");
                //clientSocket.Shutdown(SocketShutdown.Send);
                //break;
                //cts.Cancel();

                await Task.Delay(TimeSpan.FromSeconds(Settings.SendInterval));
            }
        }

        private async Task<string> SendEndMessage(ulong totalBytesSent, int processesCount)
        {
            var transEndMessage = $"Transmission completed. Processes captured: {processesCount}. Total bytes sent: {totalBytesSent}";            
            var endMessageBytes = Encoding.UTF8.GetBytes(transEndMessage);

            var length = IPAddress.HostToNetworkOrder(endMessageBytes.Length);
            var lengthBytes = BitConverter.GetBytes(length);

            //await clientSocket.SendAsync(lengthBytes, SocketFlags.None, cts.Token);
            //await clientSocket.SendAsync(endMessageBytes, SocketFlags.None, cts.Token);
            return transEndMessage;
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
