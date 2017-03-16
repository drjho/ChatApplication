using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    public class ChatServer
    {
        //private static int clientNumber = 0;

        private readonly List<ChatConnection> connections;

        private readonly object locker = new object();

        private bool isShuttingDown;

        public bool IsShuttingDown
        {
            get { lock (locker) { return isShuttingDown; } }
            set { lock (locker) { isShuttingDown = value; } }
        }

        private readonly List<string> messages;

        private Logger Logger { get; set; }

        public bool Running { get; private set; }

        public ChatServer()
        {
            messages = new List<string>();
            connections = new List<ChatConnection>();
            Logger = new Logger();
            Logger.Log(LogLevel.Info, "New log");
        }

        private void ShowAndLog(LogLevel level, string log)
        {
            lock (messages)
            {
                messages.Add(log);
            }
            Logger.Log(LogLevel.Info, log);
        }

        /// <summary>
        /// Find the ip address for localhost (for the client to connection to).
        /// </summary>
        /// <returns></returns>
        public IPAddress GetIPAddress()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);

            for (int i = 0; i < ipHostInfo.AddressList.Length; ++i)
            {
                if (ipHostInfo.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipHostInfo.AddressList[i];
                }
            }
            return null;
        }

        public void StartServer(int port)
        {
            //IPAddress localAddr = IPAddress.Parse("127.0.0.1");

            // Instantiate a server using TcpListener on local IP and port.
            var server = new TcpListener(GetIPAddress(), port);

            // Starting the server.
            server.Start();

            Running = true;

            // Show and log info
            var msg = $"Server listening to {server.LocalEndpoint.ToString()}";
            ShowAndLog(LogLevel.Info, msg);

            // Set server to accept client mode.
            server.BeginAcceptTcpClient(AcceptCallback, server);
        }

        public void StopServer(string reason)
        {
            Running = false;
            lock (connections)
            {
                var msg = $"/q {reason}";
                foreach (var connection in connections)
                {
                    connection.BeginWrite(WriteCallback, connection, msg);

                }
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            var server = (TcpListener)ar.AsyncState;
            try
            {
                var client = server.EndAcceptTcpClient(ar);
                var connection = new ChatConnection(client)
                {
                    EndPoint = client.Client.RemoteEndPoint.ToString()
                };
                connections.Add(connection);

                var evMsg = "connection from " + connection.UserName;
                ShowAndLog(LogLevel.Info, msg);


                connection.BeginRead(ReadCallback, connection);

                // Set server to accept client mode.
                server.BeginAcceptTcpClient(AcceptCallback, server);
            }
            catch (Exception ex)
            {
                Console.WriteLine("In AcceptCallback: " + ex);
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                var connection = (ChatConnection)ar.AsyncState;
                if (connection.Client.Connected)
                {
                    // The length of the message, used in getting the message.
                    var length = connection.Client.GetStream().EndRead(ar);

                    // Get the message sent by the client.
                    var message = connection.ReadBufferAndReset(length);

                    // Parse the message from client.
                    ParseMessage(message, connection);

                    // Back to read mode.
                    connection.BeginRead(ReadCallback, connection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("In ReadCallback: " + ex);
            }
        }

        private void ParseMessage(string message, ChatConnection connection)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
            var lines = message.Split(' ');
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                Disconnect(connection);
            }
            else if (command.Equals("/n"))
            {
                connection.UserName = lines[1];
                var msg = $"{connection.EndPoint} renamed to {connection.UserName}";
                ShowAndLog(LogLevel.Info, msg);
            }
        }

        private void Disconnect(ChatConnection disconnecting)
        {
            lock (connections)
            {
                connections.Remove(disconnecting);
            }
            disconnecting.CloseConnection();

            var msg = $"{disconnecting.UserName} left";
            ShowAndLog(LogLevel.Info, msg);


            SendUsernames();
        }

        private void SendUsernames()
        {
            var msg = $"/i {GetUsernamesAsString()}";
            foreach (var connection in connections)
            {
                connection.BeginWrite(WriteCallback, connection, msg);
            }
        }

        private void WriteCallback(IAsyncResult ar)
        {
            try
            {
                var connection = (ChatConnection)ar.AsyncState;
                if (connection.Client.Connected)
                {
                    connection.Client.GetStream().EndWrite(ar);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("In ReadCallback: " + ex);
            }
        }

        private string GetUsernamesAsString()
        {
            return string.Join(";", connections.Select(c => c.UserName));
        }

        public void UpdateView()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            Console.SetCursorPosition(0, 3);
            Console.WriteLine("ServerMessage:");
            foreach (var message in messages)
            {
                Console.WriteLine(message);
            }
            Console.WriteLine("----------");
            Console.SetCursorPosition(left, top);

        }

        public void ParseInput(string input)
        {
            var lines = input.Split(' ');
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                StopServer("Admin is shutting down the server.");
            }
        }

    }
}
