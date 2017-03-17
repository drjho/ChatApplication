using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApplication
{
    public class ChatClient
    {
        private readonly object locker = new object();

        public string ClientName { get; set; }

        private List<string> OtherClients { get; set; }

        private readonly List<string> chatMessages;

        private ChatConnection Connection { get; set; }

        public bool Connected => Connection.Client.Connected;

        /// <summary>
        /// Once reading is completed, it will signal end of read, 
        /// then get the message from the buffer in ChatConnection.
        /// The message is parsed and the view is always updated afterwards.
        /// Then set the connection to listen mode (BeginRead).
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                var connection = (ChatConnection)ar.AsyncState;
                if (connection.Client.Connected)
                {
                    var length = connection.Client.GetStream().EndRead(ar);

                    // Get the message sent by the client.
                    var message = connection.ReadBufferAndReset(length);

                    // Parse the message from client.
                    ParseMessage(message);

                    // Update the view of the chat.
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
        /// Show the other users and received messages.
        /// </summary>
        public void UpdateView()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            Console.SetCursorPosition(0, 3);

            Console.WriteLine("Chatters:");
            lock (OtherClients)
            {
                foreach (var clientName in OtherClients)
                {
                    Console.WriteLine(clientName);
                }
            }
            Console.WriteLine("----------");
            Console.WriteLine("ServerMessage:");
            foreach (var message in GetLastTenMessages())
            {
                Console.WriteLine(message);
            }
            Console.WriteLine("----------");
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Constructor; setting up lists of messages and users' names.
        /// Moreover, start connecting to the server.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="name"></param>
        public ChatClient(IPAddress address, int port, string name)
        {
            chatMessages = new List<string>();
            OtherClients = new List<string>();
            ClientName = name;
            var client = new TcpClient();
            client.BeginConnect(address, port, ConnectCallback, client);
        }

        /// <summary>
        /// When connected to the server, create a ChatConnection to handle read/write to stream.
        /// Start to listen for messages from server with BeginRead.
        /// At last, send the current user name to the server for update.
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var client = (TcpClient)ar.AsyncState;
                client.EndConnect(ar);
                Connection = new ChatConnection(client) { UserName = ClientName };
                Connection.BeginRead(ReadCallback, Connection);

                Connection.BeginWrite(WriteCallback, Connection, $"/r {ClientName}");


            }
            catch (Exception ex)
            {
                Console.WriteLine("In ConnectCallback: " + ex);
            }
        }

        /// <summary>
        /// Signal end of writing.
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
                Console.WriteLine("In WriteCallback: " + ex);
            }
        }

        /// <summary>
        /// Parse messages: if "/q" then Server is disconnecting
        /// if "/i" then Server send updated other users' names
        /// else message from Server or other users
        /// </summary>
        /// <param name="message"></param>
        private void ParseMessage(string message)
        {
            var lines = message.Split(' ').ToList();
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                if (lines.Count > 1)
                    chatMessages.Add("Server: " + string.Join(" ", lines.GetRange(1, lines.Count - 1)));
                Connection.CloseConnection();
            }
            else if (command.Equals("/i"))
            {
                if (lines.Count == 2)
                {
                    //Console.WriteLine(lines[1]);
                    var names = lines[1].Split(';');
                    UpdateOtherClientNames(names);
                }
            }
            else
            {
                lock (chatMessages)
                {
                    chatMessages.Add(message);
                }
            }
        }

        /// <summary>
        /// Parse the input from current user.
        /// if "/q" then disconnect from the server and close connection
        /// else send the input to server to handle.
        /// </summary>
        /// <param name="input"></param>
        public void ParseInput(string input)
        {
            var lines = input.Split(' ');
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                Quit();
            }
            else
            {
                Connection.BeginWrite(WriteCallback, Connection, input);
            }
        }

        /// <summary>
        /// When the current client disconnect from the server and close the connection.
        /// </summary>
        public void Quit()
        {
            Connection.BeginWrite(WriteCallback, Connection, "/q");
            Connection.CloseConnection();
        }

        /// <summary>
        /// Extract the last ten received messages 
        /// </summary>
        /// <returns></returns>
        public List<string> GetLastTenMessages()
        {
            lock (chatMessages)
            {
                var start = Math.Max(0, chatMessages.Count - 10);
                var count = Math.Min(10, chatMessages.Count);
                return chatMessages.GetRange(start, count);
            }
        }

        /// <summary>
        /// Update the users' names.
        /// </summary>
        /// <param name="clients"></param>
        private void UpdateOtherClientNames(ICollection<string> clients)
        {
            lock (OtherClients)
            {
                OtherClients = clients.ToList();
            }
        }
    }
}
