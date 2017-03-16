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

        private Dictionary<string, ChatConnection> ClientRegistry { get; set; } // Use concurrentDictionary instead?

        private readonly object locker = new object();

        private bool isShuttingDown;

        public bool IsShuttingDown
        {
            get { lock (locker) { return isShuttingDown; } }
            set { lock (locker) { isShuttingDown = value; } }
        }

        private List<string> messages;

        public List<string> Messages
        {
            get { lock (locker) { return messages; } }
            set { lock (locker) { messages = value; } }
        }

        private Logger Logger { get; set; }

        public ChatServer()
        {
            Messages = new List<string>();
            ClientRegistry = new Dictionary<string, ChatConnection>();
            Logger = new Logger();
            Logger.Log(LogLevel.Info, "New log");
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

            // Show and log info
            var msg = $"Server listening to {server.LocalEndpoint.ToString()}";
            Messages.Add(msg);
            Logger.Log(LogLevel.Info, msg);

            // Set server to accept client mode.
            server.BeginAcceptTcpClient(AcceptCallback, server);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            var server = (TcpListener)ar.AsyncState;
            try
            {
                var client = server.EndAcceptTcpClient(ar);
                var connection = new ChatConnection(client.Client.RemoteEndPoint.ToString(), client);
                ClientRegistry[connection.Name] = connection;

                var evMsg = "connection from " + connection.Name;
                Logger.Log(LogLevel.Info, evMsg);
                Messages.Add(evMsg);

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
        }

        //public async Task SendToAllAsync(string message, string sender = "")
        //{
        //    await Task.WhenAll(ClientRegistry.Where(p => p.Key != sender)
        //        .Select(p => p.Value.WriteMessageAsync(message)));
        //}

        //public async Task SendToAsync(string sender, string receiver, string message)
        //{
        //    var tmpStr = $"({sender}): {message}";
        //    await Task.WhenAll(ClientRegistry.Where(p => p.Key == receiver)
        //        .Select(p => p.Value.WriteMessageAsync(message)));
        //}

        //private string GetClientNamesAsMessage => "/list " + string.Join(" ", ClientRegistry.Keys.ToList());

        //private async Task ListenToClient(ChatConnection connection)
        //{
        //    string evMsg;
        //    while (true)
        //    {
        //        string message = await connection.ReadMessageAsync();
        //        Logger.Log(LogLevel.Info, message);
        //        if (message != null)
        //        {
        //            var lines = message.Split(' ');
        //            var command = lines[0].ToLowerInvariant();
        //            if (command.Equals("/name"))
        //            {
        //                // format /name <new name>
        //                // TODO: register client, send new client list to all, 
        //                //          announce this new client.
        //                if (!ClientRegistry.ContainsKey(lines[1]))
        //                {
        //                    ClientRegistry.Remove(connection.Name);
        //                    connection.Name = lines[1];
        //                    ClientRegistry[lines[1]] = connection;
        //                    await SendToAllAsync(GetClientNamesAsMessage);


        //                    evMsg = lines[1] + " joined";
        //                    Logger.Log(LogLevel.Info, evMsg);
        //                    Messages.Add(evMsg);
        //                    await SendToAllAsync(evMsg, lines[1]);

        //                }
        //            }
        //            else if (command.Equals("/disconnect"))
        //            {
        //                // format /disconnect <name>
        //                // TODO: unregsiter client, close stream, send new client list to all,
        //                //          announce client leave.
        //                evMsg = lines[1] + " is leaving";
        //                Logger.Log(LogLevel.Info, evMsg);
        //                Messages.Add(evMsg);

        //                ClientRegistry[lines[1]].CloseConnection();
        //                ClientRegistry.Remove(lines[1]);

        //                await SendToAllAsync(GetClientNamesAsMessage);
        //                await SendToAllAsync(evMsg, lines[1]);

        //                break;
        //            }
        //            else if (command.Equals("/whisper"))
        //            {
        //                // format /whisper <sender> <receiver> <message>
        //                // TODO: find receiver in register, if exist send message or return error message.
        //                var msg = string.Join(" ", lines.ToList().GetRange(3, lines.Length - 3));
        //                evMsg = $"{lines[1]} -> {lines[2]} : {msg}";
        //                if (ClientRegistry.ContainsKey(lines[2]))
        //                {
        //                    evMsg = $"success: {evMsg}";
        //                    Logger.Log(LogLevel.Info, evMsg);
        //                    Messages.Add(evMsg);

        //                    await SendToAsync(lines[1], lines[2], msg);
        //                }
        //                else
        //                {
        //                    evMsg = $"error: {evMsg}";
        //                    Logger.Log(LogLevel.Info, evMsg);
        //                    Messages.Add(evMsg);
        //                    await SendToAsync("Server", lines[1], $"{lines[2]} not in chat");
        //                }
        //            }
        //            else
        //            {
        //                // format <sender> <message> 
        //                // TODO: send the message to everyone except sender.
        //                evMsg = $"{lines[1]} -> all : {lines[2]}";
        //                evMsg = $"success: {evMsg}";
        //                Logger.Log(LogLevel.Info, evMsg);
        //                Messages.Add(evMsg);
        //                await SendToAllAsync(lines[2], lines[1]);
        //            }
        //        }
        //    }
        //}

        //public async Task ProcessClient(TcpClient client)
        //{
        //    var evMsg = client.Client.RemoteEndPoint.ToString() + " connected.";
        //    Logger.Log(LogLevel.Info, evMsg);
        //    Messages.Add(evMsg);
        //    var name = $"client{clientNumber++}";
        //    ClientRegistry[name] = new ChatConnection(name, client);
        //    evMsg = name + " joined";
        //    Logger.Log(LogLevel.Info, evMsg);
        //    Messages.Add(evMsg);
        //    Task.Run(() => ListenToClient(ClientRegistry[name]));

        //}
    }
}
