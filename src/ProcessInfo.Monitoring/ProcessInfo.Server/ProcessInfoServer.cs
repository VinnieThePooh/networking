using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProcessInfo.Server.Enums;
using ProcessInfo.Server.Settings;

namespace ProcessInfo.Server
{
    public class ProcessInfoServer : IDisposable
    {
        private readonly List<string> allMessages = new();
        private Socket clientSocket;
        private int counter;
        private CancellationTokenSource cts;

        private readonly List<string> internalBatch = new();

        public ProcessInfoServer(ServerSettings settings)
        {
            Settings = settings;
        }

        public ServerSettings Settings { get; }

        public void Dispose()
        {
            cts.Dispose();
            clientSocket?.Dispose();
        }

        public event EventHandler<ProcessInfoReceivedEventArgs> ProcessInfoReceived;

        //only single client is able to connect
        public async Task StartListening()
        {
            cts = new CancellationTokenSource();

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(Settings.ServerAddress), Settings.ServerPort);
            var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                serverSocket.Bind(tcpEndPoint);
                serverSocket.Listen();
                Console.WriteLine("Listening incoming connections..");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex);

                throw;
            }

            clientSocket = serverSocket.Accept();
            var remoteEp = (IPEndPoint)clientSocket.RemoteEndPoint;
            Console.WriteLine($"Connnected client: {remoteEp.Address}:{remoteEp.Port}");

            Memory<byte> memory = null;
            var leftMemory = Memory<byte>.Empty;

            var readInitial = 0;
            var nextLength = 0;
            var offset = 0;
            var counter = 0;

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    memory = new Memory<byte>(new byte[4096]);
                    offset = !leftMemory.IsEmpty ? leftMemory.Length : 0;
                    leftMemory.CopyTo(memory);
                    readInitial = await clientSocket.ReceiveAsync(memory[offset..], SocketFlags.None, cts.Token);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Dispose();
                }

                if (readInitial == 0)
                {
                    //client disconnected prematurely

                    if (internalBatch.Any())
                    {
                        ProcessInfoReceived?.Invoke(this, new ProcessInfoReceivedEventArgs(internalBatch));
                        internalBatch.Clear();
                    }

                    cts.Cancel();

                    break;
                }

                (leftMemory, nextLength) = TryReadMessages(memory[..(offset + readInitial)], readInitial, nextLength);
            }
        }

        private (Memory<byte> LeftMemory, int NextDataLength) TryReadMessages(Memory<byte> buffer, int readBytes,
            int nextLength)
        {
            var offset = 0;
            var length = 0;

            if (nextLength != 0)
            {
                length = nextLength;
            }
            else
            {
                length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer[offset..4].ToArray()));
                offset = 4;
            }

            if (readBytes == 4)
                return (Memory<byte>.Empty, length);

            var end = offset + length;

            while (end < buffer.Length)
            {
                var message = Encoding.UTF8.GetString(buffer[offset..end].Span);
                allMessages.Add(message);

                if (message.Contains("Transmission completed."))
                    Debugger.Break();

                Console.WriteLine($"[{DateTime.Now}]: {++counter}. Message received: {message}");

                if (Settings.NotificationMode == NotificationMode.Batch)
                {
                    internalBatch.Add(message);

                    if (internalBatch.Count % Settings.NotificationBatchSize == 0)
                    {
                        ProcessInfoReceived?.Invoke(this, new ProcessInfoReceivedEventArgs(internalBatch));
                        internalBatch.Clear();
                    }
                }
                else
                {
                    ProcessInfoReceived?.Invoke(this, new ProcessInfoReceivedEventArgs(message));
                }

                offset = end;

                if (offset + 4 < buffer.Length)
                {
                    length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer[offset..(offset + 4)].ToArray()));
                    offset += 4;
                    end = offset + length;
                }
                else
                {
                    return (buffer.Slice(offset, buffer.Length - offset), 0);
                }
            }

            if (end > buffer.Length)
                return (buffer.Slice(offset, buffer.Length - offset), length);

            return (Memory<byte>.Empty, 0);
        }

        public Task StopListening()
        {
            if (cts.IsCancellationRequested)
                return Task.CompletedTask;

            cts.Cancel();

            return Task.CompletedTask;
        }
    }
}