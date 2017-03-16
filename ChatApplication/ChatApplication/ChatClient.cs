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

        private List<string> Messages { get; set; }

        private ChatConnection Connection { get; set; }

        public bool Connected => Connection.Client.Connected;

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

        public ChatClient(IPAddress address, int port, string name)
        {
            Messages = new List<string>();
            OtherClients = new List<string>();
            ClientName = name;
            var client = new TcpClient();
            client.BeginConnect(address, port, ConnectCallback, client);
        }

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

        private void ParseMessage(string message)
        {
            var lines = message.Split(' ');
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                Connection.CloseConnection();
            }
            else if (command.Equals("/i"))
            {
                if (lines.Length == 2)
                {
                    //Console.WriteLine(lines[1]);
                    var names = lines[1].Split(';');
                    UpdateOtherClientNames(names);
                }
            }
            UpdateView();
        }

        public void ParseInput(string input)
        {
            var lines = input.Split(' ');
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                Quit();
            }
            else if (command.Equals("/u"))
            {
                Connection.BeginWrite(WriteCallback, Connection, "/u");
            }
        }

        public void Quit()
        {
            Connection.BeginWrite(WriteCallback, Connection, "/q");
            Connection.CloseConnection();
        }

        public List<string> GetLastTenMessages()
        {
            lock (Messages)
            {
                var start = Math.Max(0, Messages.Count - 10);
                var count = Math.Min(10, Messages.Count);
                return Messages.GetRange(start, count);
            }
        }

        private void UpdateOtherClientNames(ICollection<string> clients)
        {
            lock (OtherClients)
            {
                OtherClients = clients.ToList();
            }
        }
    }
}
