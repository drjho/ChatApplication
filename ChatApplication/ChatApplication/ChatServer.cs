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
        private readonly List<ChatConnection> connections;

        private readonly List<string> messages;

        private Logger Logger { get; set; }

        public bool Running { get; private set; }

        /// <summary>
        /// Constructor 
        /// </summary>
        public ChatServer()
        {
            messages = new List<string>();
            connections = new List<ChatConnection>();
            Logger = new Logger();
            Logger.Log(LogLevel.Info, "New log");
        }

        /// <summary>
        /// Add a message to the history for future show
        /// Also log the message to file.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="log"></param>
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

        /// <summary>
        /// The server is a tcpListener which initiate with local IP and provided port.
        /// The EndPoint of the server is shown for manual read.
        /// Finally, the server start accepting incoming connection request.
        /// </summary>
        /// <param name="port"></param>
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

        /// <summary>
        /// Stopping the server and the loop of the server program for admin input.
        /// Tell the users to disconnect from the current server.
        /// Then close down the logger.
        /// </summary>
        /// <param name="reason"></param>
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
            Logger.CloseLog();
        }

        /// <summary>
        /// When a tcpClient is accepted, we set up a new ChatConnection
        /// and use that for stream read/ write. The ChatConnection is then set to start listening to.
        /// At the end, the server is set to accept further tcpClient.
        /// </summary>
        /// <param name="ar"></param>
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

                var evMsg = "connection from " + connection.EndPoint;
                ShowAndLog(LogLevel.Info, evMsg);

                connection.BeginRead(ReadCallback, connection);

                // Set server to accept client mode.
                server.BeginAcceptTcpClient(AcceptCallback, server);
            }
            catch (Exception ex)
            {
                Console.WriteLine("In AcceptCallback: " + ex);
            }
        }

        /// <summary>
        /// When a message is read, it will signal the end of read.
        /// Get the message from the buffer in the ChatConnection.
        /// Parse the message and update the view, showing event messages
        /// Set it to listen again at the end.
        /// </summary>
        /// <param name="ar"></param>
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

                    // Update the view of the server.
                    UpdateView();

                    // Back to read mode.
                    connection.BeginRead(ReadCallback, connection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("In ReadCallback: " + ex);
            }
        }

        /// <summary>
        /// Can handle disconnect of client ("/q"),
        /// can handle rename of user name ("/r"),
        /// can handle user names request ("/u"),
        /// can handle whisper from one user to another ("/w"), and
        /// can handle send message to all ("/a").
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        private void ParseMessage(string message, ChatConnection sender)
        {
            var lines = message.Split(' ').ToList();
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                Disconnect(sender);
                var msg = $"/i {GetUsernamesAsString()}";
                lock (connections)
                {
                    foreach (var connection in connections)
                    {
                        connection.BeginWrite(WriteCallback, connection, msg);
                    }
                }
            }
            else if (command.Equals("/r"))
            {
                if (lines.Count == 2)
                {
                    sender.UserName = lines[1];
                    var evMsg = $"{sender.EndPoint} renamed to {sender.UserName}";
                    ShowAndLog(LogLevel.Info, evMsg);
                    var msg = $"/i {GetUsernamesAsString()}";
                    lock (connections)
                    {
                        foreach (var connection in connections)
                        {
                            connection.BeginWrite(WriteCallback, connection, evMsg);
                        }
                    }
                }
            }
            else if (command.Equals("/u"))
            {
                var msg = $"{sender.UserName} ask for usernames";
                ShowAndLog(LogLevel.Info, msg);
                sender.BeginWrite(WriteCallback, sender, $"/i {GetUsernamesAsString()}");
            }
            else if (command.Equals("/w"))
            {
                if (lines.Count > 2)
                {
                    var msg = string.Join(" ", lines.GetRange(2, lines.Count - 2));
                    var receiver = connections.FirstOrDefault(c => c.UserName == lines[1]);
                    if (receiver != null && sender.UserName != receiver.UserName)
                    {
                        var evMsg = $"{sender.UserName}->{receiver.UserName}:{msg}";
                        ShowAndLog(LogLevel.Info, evMsg);
                        receiver.BeginWrite(WriteCallback, receiver, $"{sender.UserName}:{msg}");
                        sender.BeginWrite(WriteCallback, sender, $"+ {message}");
                        return;
                    }
                }
                sender.BeginWrite(WriteCallback, sender, $"- {message}");

            }
            else if (command.Equals("/a"))
            {
                if (lines.Count > 1)
                {
                    var msg = string.Join(" ", lines.GetRange(1, lines.Count - 1));
                    var evMsg = $"{sender.UserName}->all:{msg}";
                    ShowAndLog(LogLevel.Info, evMsg);
                    msg = sender.UserName + ":" + string.Join(" ", lines.GetRange(1, lines.Count - 1));
                    foreach (var connection in connections)
                    {
                        if (connection.UserName != sender.UserName)
                            connection.BeginWrite(WriteCallback, connection, msg);
                    }
                    sender.BeginWrite(WriteCallback, sender, $"+ {message}");
                    return;
                }
                sender.BeginWrite(WriteCallback, sender, $"- {message}");
            }
        }

        /// <summary>
        /// Disconnecting a certain connection when the associated client (user) is leaving.
        /// This event is logged.
        /// </summary>
        /// <param name="disconnecting"></param>
        private void Disconnect(ChatConnection disconnecting)
        {
            lock (connections)
            {
                connections.Remove(disconnecting);
            }
            disconnecting.CloseConnection();

            var msg = $"{disconnecting.UserName} left";
            ShowAndLog(LogLevel.Info, msg);
        }

        /// <summary>
        /// Just signal end of write when writing to a client (user) is done.
        /// </summary>
        /// <param name="ar"></param>
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

        /// <summary>
        /// Format the users' names to a single string for sending thru stream.
        /// </summary>
        /// <returns></returns>
        private string GetUsernamesAsString()
        {
            lock (connections)
            {
                return string.Join(";", connections.Select(c => c.UserName));
            }
        }

        /// <summary>
        /// Showing the stored event messages in the server.
        /// </summary>
        public void UpdateView()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            Console.SetCursorPosition(0, 3);
            Console.WriteLine("ServerMessage:");
            lock (messages)
            {
                foreach (var message in messages)
                {
                    Console.WriteLine(message);
                }
            }
            Console.WriteLine("----------");
            Console.SetCursorPosition(left, top);

        }

        /// <summary>
        /// Parsing input from admin, (at the moment, just for shutting down the server).
        /// </summary>
        /// <param name="input"></param>
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
