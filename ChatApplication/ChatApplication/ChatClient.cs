using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    public class ChatClient
    {
        private readonly object locker = new object();
        public string Name { get; set; }
        private StreamWriter Writer { get; set; }
        private HashSet<string> OtherClients { get; set; }
        private IPAddress Address { get; set; }
        private int Port { get; set; }
        private List<string> Messages { get; set; }
        private ChatConnection Connection { get; set; }


        public bool Connected => Connection.Client.Connected;
        

         private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var client = (TcpClient)ar.AsyncState;
                client.EndConnect(ar);

            }
            catch (Exception ex)
            {

                Console.WriteLine("In ConnectCallback: " + ex);
            }

        }

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
                    ParseMessage(message, connection);

                    // Update the view of the chat.
                    UpdateChat();

                    // Back to read mode.
                    connection.BeginRead(ReadCallback, connection);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("In ReadCallback: " + ex);

            }
        }

        public void UpdateChat()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            Console.SetCursorPosition(0, 3);
            Console.WriteLine("Update chatter list by pressing [Enter]");
            Console.WriteLine("Chatters:");
            foreach (var clientName in GetOtherClientNames())
            {
                Console.WriteLine(clientName);
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

        private void ParseMessage(string message, ChatConnection connection)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
        }

        public ChatClient(IPAddress address, int port, string name)
        {
            Messages = new List<string>();
            OtherClients = new HashSet<string>();
            Name = name;
            var client = new TcpClient();
            client.BeginConnect(address, port, ConnectCallback, client);

        }

        public void StartClient()
        {
        


            UpdateChat();

            Connection = new ChatConnection(Name, Client);
            Connection.BeginRead(ReadCallback, Connection);

            //Client.BeginConnect(Address, port, OnCompleteConnectCallBack, Client);


            //await Client.ConnectAsync(Address, Port);
            //IsConnected = true;
            //var stream = Client.GetStream();
            //Writer = new StreamWriter(stream);
            //Writer.AutoFlush = true;

            //using (var reader = new StreamReader(stream))
            //{
            //    while (IsConnected)
            //    {
            //        string response = await reader.ReadLineAsync();
            //        var lines = response.Split(' ').ToList();
            //        var command = lines[0].ToLowerInvariant();
            //        if (command.Equals("/list"))
            //        {
            //            UpdateOtherClientNames(lines.GetRange(1, lines.Count - 1));
            //        }
            //        else if (command.Equals("/disconnect"))
            //        {
            //            IsConnected = false;
            //        }
            //        else
            //        {
            //            lock (Messages)
            //            {
            //                Messages.Add(string.Join(" ", lines.GetRange(1, lines.Count - 1)));
            //            }
            //        }
            //    }
            //}
            //Client.Close();
        }




        public void ParseInput(string input)
        {
            var lines = input.Split(' ');
            var command = lines[0].ToLowerInvariant();
            if (command.Equals("/q"))
            {
                Quit();
            }
        }

        public void Quit()
        {
            Connection.BeginWrite(null, null, "/q");
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
                OtherClients = new HashSet<string>(clients);
            }
        }

        public List<string> GetOtherClientNames()
        {
            lock (OtherClients)
            {
                return OtherClients.ToList();
            }
        }
    }
}
