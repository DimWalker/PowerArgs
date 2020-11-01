﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class SocketServerNetworkProvider : Lifetime, IServerNetworkProvider
    {
        public string ServerId { get; private set; }

        public Event<MultiPlayerClientConnection> ClientConnected { get; private set; } = new Event<MultiPlayerClientConnection>();
        public Event<string> MessageReceived { get; private set; } = new Event<string>();

        private Dictionary<string, RemoteSocketConnection> connections = new Dictionary<string, RemoteSocketConnection>();
        private TcpListener listener;
        private bool isListening;
        private TaskCompletionSource<bool> listeningDeferred;
        private int port;
        private IPHostEntry ipHostInfo;
        private IPEndPoint localEP;

        public ServerInfo ServerInfo => new ServerInfo()
        {
            Server = Dns.GetHostName(),
            Port = port,
        };

        public SocketServerNetworkProvider(int port)
        {
            this.port = port;
            this.ServerId = "http://" + Dns.GetHostName() + ":" + port;
        }

        public Task OpenForNewConnections()
        {
            isListening = true;
            listeningDeferred = new TaskCompletionSource<bool>();
            var startListeningDeferred = new TaskCompletionSource<bool>();
            BackgroundThread t = null;
            t = new BackgroundThread(() =>
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                }
                catch (Exception ex)
                {
                    startListeningDeferred.SetException(ex);
                    return;
                }

                // we have started listening
                startListeningDeferred.SetResult(true);

                try
                {
                    while (isListening && t.IsExpired == false)
                    {
                        Socket socket;
                        try
                        {
                            socket = listener.AcceptSocket(TimeSpan.FromSeconds(1));
                            if (t.IsExpired) return;
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                        var connection = new RemoteSocketConnection()
                        {
                            ClientId = (socket.RemoteEndPoint as IPEndPoint).Address.ToString() + ":" + (socket.RemoteEndPoint as IPEndPoint).Port,
                            RemoteSocket = socket,
                            MessageReceived = this.MessageReceived,
                        };
                        this.OnDisposed(connection.Dispose);
                        connection.Listen();
                        connections.Add(connection.ClientId, connection);
                        ClientConnected.Fire(connection);
                    }
                    listener.Stop();
                    listeningDeferred.SetResult(true);
                }
                catch(Exception ex)
                {
                    listeningDeferred.SetException(ex);
                }
                finally
                {
                    listeningDeferred = null;
                }
            });
            this.OnDisposed(t.Dispose);
            t.Start();
            return startListeningDeferred.Task;
        }

        public Task CloseForNewConnections()
        {
            isListening = false;
            return listeningDeferred.Task;
        }

        public void SendMessageToClient(string message, MultiPlayerClientConnection client)
        {
            lock (client)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);
                var sent = (client as RemoteSocketConnection).RemoteSocket.Send(lengthBytes);
                if (sent != lengthBytes.Length) throw new Exception("WTF");
                sent = (client as RemoteSocketConnection).RemoteSocket.Send(bytes);
                if (sent != bytes.Length) throw new Exception("WTF");
            }
        }

        protected override void DisposeManagedResources() { }
    }

    public class RemoteSocketConnection : MultiPlayerClientConnection
    {
        public Event<Exception> UnexpectedDisconnect { get; private set; } = new Event<Exception>();
        public Socket RemoteSocket { get; set; }

        public Event<string> MessageReceived { get; set; }

        public Task Listen() => new BackgroundThread(ListenThread).Start();
        
        private void ListenThread()
        {
            try
            {
                RemoteSocket.ReceiveTimeout = 1000;
                byte[] buffer = new byte[1024 * 1024];
                while (this.IsExpired == false)
                {
                    SocketHelpers.Read(this, RemoteSocket, buffer, 4);
                    if (this.IsExpired) break;
                    var messageLength = BitConverter.ToInt32(buffer, 0);
                    SocketHelpers.Read(this, RemoteSocket, buffer, messageLength);
                    if (this.IsExpired) break;
                    var messageText = Encoding.UTF8.GetString(buffer, 0, messageLength);
                    MessageReceived.Fire(messageText);
                }
            }
            catch(Exception ex)
            {
                UnexpectedDisconnect.Fire(ex);
                this.Dispose();
            }
            finally
            {
                RemoteSocket.Close();
            }
        }
    }
}
