using ProcessInfo.Server.Settings;
using System;
using System.Diagnostics;
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
                    cts.Cancel();
                    break;
                }                                  
                

                try
                {                                      
                    (succeeded, leftMemory, nextLength)  = TryReadMessages(memory, readInitial, nextLength);             
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
            var length =  nextLength != 0 ? nextLength: IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer[offset..4].ToArray()));

            if (readBytes == 4)
                return (false, Memory<byte>.Empty, length);

            offset = nextLength != 0 ? 0 : 4;
            var end = offset + length;

            while (end < readBytes)
            {
                var message = Encoding.UTF8.GetString(buffer[offset..end].Span);
                Console.WriteLine($"[{DateTime.Now}]: {++counter}. Message received: {message}");
                ProcessInfoReceived?.Invoke(this, new ProcessInfoReceivedEventArgs (message));

                offset = end;
                if (offset + 4 < readBytes)
                {                    
                    length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer[offset..(offset+4)].ToArray()));
                    offset = offset + 4;
                    end = offset + length;
                }
                else
                {
                    return (true, buffer.Slice(offset, readBytes - offset), 0);
                }                
            }
            return (true, buffer.Slice(offset, readBytes - offset), offset + 4 < readBytes ? length : 0);
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
