using ProcessInfo.Server.Enums;
using ProcessInfo.Server.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessInfo.Server
{
    public class ProcessInfoServer: IDisposable
    {
        public event EventHandler<ProcessInfoReceivedEventArgs> ProcessInfoReceived;

        public ServerSettings Settings { get; }
        private CancellationTokenSource cts;
        private Socket serverSocket;
        private Socket clientSocket;
        private int counter = 0;

        private List<string> internalBatch = new List<string>();

        public ProcessInfoServer(ServerSettings settings)
        {
            Settings = settings;
        }

        //only single client is able to connect
        public async Task StartListening()
        {
            cts = new CancellationTokenSource();

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(Settings.ServerAddress), Settings.ServerPort);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                serverSocket.Bind(tcpEndPoint);
                serverSocket.Listen();
                Console.WriteLine("Listening incoming connections..");
            }
            catch (SocketException  ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            clientSocket = serverSocket.Accept();
            var remoteEp = (IPEndPoint)clientSocket.RemoteEndPoint;
            Console.WriteLine($"Connnected client: {remoteEp.Address}:{remoteEp.Port}");

            Memory<byte> memory = null;
            Memory<byte> leftMemory = Memory<byte>.Empty;
            
            int readInitial = 0;
            bool succeeded;
            int nextLength = 0;
            int offset = 0;
            int counter = 0;

            while (!cts.IsCancellationRequested)
            {
                try
                {   
                    memory = new Memory<byte>(new byte[4096]);
                    offset = !leftMemory.IsEmpty ? leftMemory.Length : 0;
                    Debug.WriteLine($"{++counter}. leftMemory.Length: {leftMemory.Length}");
                    leftMemory.CopyTo(memory);                 

                    readInitial = await clientSocket.ReceiveAsync(memory[offset..], SocketFlags.None, cts.Token);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Dispose();
                }

                if (readInitial == 0)
                {
                    //client disconnected prematurely
                    //add message here

                    if (internalBatch.Any())
                    {
                        ProcessInfoReceived?.Invoke(this, new ProcessInfoReceivedEventArgs(internalBatch));
                        internalBatch.Clear();
                    }

                    cts.Cancel();
                    break;
                }                                  
                

                try
                {                                      
                    (succeeded, leftMemory, nextLength)  = TryReadMessages(memory[0..(offset+readInitial)], readInitial, nextLength);             
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Dispose();
                }                          
            }
        }
        
        // true - если было прочитано хотя бы одно сообщение
        private (bool Succeeded, Memory<byte> LeftMemory, int NextDataLength) TryReadMessages(Memory<byte> buffer, int readBytes, int nextLength)
        {
            int offset = 0;
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

            Debug.WriteLine($"Offset: {offset}. Bytes read: {readBytes}. Buffer length: {buffer.Length}");

            if (readBytes == 4)
                return (false, Memory<byte>.Empty, length);
            
            var end = offset + length;

            while (end < buffer.Length)
            {
                var message = Encoding.UTF8.GetString(buffer[offset..end].Span);
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
                    length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer[offset..(offset+4)].ToArray()));
                    if (length > 100)
                        Debugger.Break();
                    offset = offset + 4;
                    end = offset + length;
                }
                else
                {
                    return (true, buffer.Slice(offset, buffer.Length - offset), 0);
                }                
            }

            if (end > buffer.Length)
                return (true, buffer.Slice(offset, buffer.Length - offset), length);
            return (true, Memory<byte>.Empty, 0);
        }

        public Task StopListening()
        {
            var completed = Task.CompletedTask;
            
            if (cts.IsCancellationRequested)
                return completed;

            cts.Cancel();
            return completed;
        }

        public void Dispose()
        {
            cts.Dispose();
            serverSocket.Dispose();
            clientSocket?.Dispose();
        }
    }
}
