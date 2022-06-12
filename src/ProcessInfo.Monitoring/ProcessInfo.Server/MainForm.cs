﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProcessInfo.Server.Settings;
using System.Net;

namespace ProcessInfo.Server
{
    public partial class MainForm : Form
    {
        ProcessInfoServer server;

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
            richTextBox1.AppendText($"{e.ProcessInfo}\n");
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                await server.StartListening();
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