using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using ProcessInfo.Server.Enums;
using ProcessInfo.Server.Settings;

namespace ProcessInfo.Server
{
    public partial class MainForm : Form
    {
        private readonly ProcessInfoServer server;

        public MainForm(ServerSettings settings)
        {
            InitializeComponent();
            server = new ProcessInfoServer(settings);
            server.ProcessInfoReceived += Server_ProcessInfoReceived;
            Closing += MainForm_Closing;
        }

        private async void MainForm_Closing(object sender, CancelEventArgs e)
        {
            await server.StopListening();
            server.Dispose();
        }

        private void Server_ProcessInfoReceived(object? sender, ProcessInfoReceivedEventArgs e)
        {
            switch (server.Settings.NotificationMode)
            {
                case NotificationMode.Single:
                    richTextBox1.AppendText($"{e.ProcessInfo}\n");

                    break;
                case NotificationMode.Batch:
                    richTextBox1.AppendText($"{string.Join("\n", e.ProcessInfos)}");

                    break;
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                await server.StartListening();
            }
            catch (OperationCanceledException oce)
            {
                Console.WriteLine("Cancelled listening");
            }
            catch (ProtocolViolationException pve)
            {
                Console.WriteLine(pve.Message);
                Environment.Exit(-1);
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine(e);
            }
            catch (SocketException exception)
            {
                Console.WriteLine(exception);
                Environment.Exit(exception.ErrorCode);
            }
        }
    }
}