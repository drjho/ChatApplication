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
    class ChatServer
    {
        private Dictionary<string, ChatConnection> ClientRegistry { get; set; } // Use concurrentDictionary instead?

        public ChatServer()
        {
            ClientRegistry = new Dictionary<string, ChatConnection>();
            GetIPAddress();
            Console.WriteLine("Server started...");
        }

        public void GetIPAddress()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);

            for (int i = 0; i < ipHostInfo.AddressList.Length; ++i)
            {
                if (ipHostInfo.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine(ipHostInfo.AddressList[i]);
                }
            }
        }

        public async Task StartServer(int port)
        {
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            var server = new TcpListener(localAddr, port);
            server.Start();
            Console.WriteLine("Server listening...");
            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                await ProcessClient(client);
            }
        }

        public async Task SendToAllAsync(string message, string sender = "")
        {
            await Task.WhenAll(ClientRegistry.Where(p => p.Key != sender)
                .Select(p => p.Value.WriteLineAsync(message)));
        }

        public async Task SendToAsync(string sender, string receiver, string message)
        {
            var tmpStr = $"({sender}): {message}";
            await Task.WhenAll(ClientRegistry.Where(p => p.Key == receiver)
                .Select(p => p.Value.WriteLineAsync(message)));
        }

        private bool TryRegisterClient(string name, TcpClient client)
        {
            var connection = new ChatConnection(name, client);
            if (!ClientRegistry.ContainsKey(name))
            {
                ClientRegistry[name] = connection;
                return true;
            }
            else
            {
                connection.WriteLineAsync($"{name} is taken.");
                return false;
            }
        }

        private string GetClientNamesAsMessage => "/list " + string.Join(" ", ClientRegistry.Keys.ToList());

        public async Task ProcessClient(TcpClient client)
        {
            Console.WriteLine(client.Client.RemoteEndPoint.ToString());
            try
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream);
                writer.AutoFlush = true;

                while (true)
                {
                    string message = await reader.ReadLineAsync();
                    if (message != null)
                    {
                        var lines = message.Split(' ');
                        var command = lines[0].ToLowerInvariant();
                        if (command.Equals("/join"))
                        {
                            // format /join <name>
                            // TODO: register client, send new client list to all, 
                            //          announce this new client.
                            var evMsg = lines[1] + " joined";
                            Console.WriteLine(evMsg);
                            TryRegisterClient(lines[1], client);
                            //ClientRegistry[lines[1]] = new ChatConnection(lines[1], client); // maybe more than just saving the stream.
                            // Class that includes, name, reader, writer.
                            await SendToAllAsync(GetClientNamesAsMessage, lines[1]);
                            await SendToAllAsync(evMsg, lines[1]);
                        }
                        else if (command.Equals("/disconnect"))
                        {
                            // format /disconnect <name>
                            // TODO: unregsiter client, close stream, send new client list to all,
                            //          announce client leave.
                            var evMsg = lines[1] + " is leaving";
                            Console.WriteLine(evMsg);
                            ClientRegistry[lines[1]].CloseConnection();
                            ClientRegistry.Remove(lines[1]);
                            // send new client list!
                            await SendToAllAsync(evMsg, lines[1]);
                            break;
                        }
                        else if (command.Equals("/whisper"))
                        {
                            // format /whisper <sender> <receiver> <message>
                            // TODO: find receiver in register, if exist send message or return error message.
                            var evMsg = $"{lines[1]} -> {lines[2]} : {lines[3]}";
                            if (ClientRegistry.ContainsKey(lines[2]))
                            {
                                Console.WriteLine($"success: {evMsg}");
                                await SendToAsync(lines[1], lines[2], lines[3]);
                            }
                            else
                            {
                                Console.WriteLine($"error: {evMsg}");
                                await writer.WriteLineAsync($"{lines[2]} not in chat");
                                //await SendToAsync("Server", lines[1], $"{lines[2]} not in chat");
                            }
                        }
                        else
                        {
                            // format <sender> <message> 
                            // TODO: send the message to everyone except sender.
                            var evMsg = $"{lines[1]} -> all : {lines[2]}";
                            Console.WriteLine($"success: {evMsg}");
                            await SendToAllAsync(lines[2], lines[1]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (client.Connected)
                    client.Close();

            }
        }
    }
}
