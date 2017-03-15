using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    class ChatClient
    {
        private readonly object locker = new object();
        private StreamWriter Writer { get; set; }
        private HashSet<string> OtherClients { get; set; }
        private IPAddress Address { get; set; }
        private int Port { get; set; }
        private TcpClient Client { get; set; }
        private List<string> Messages { get; set; }

        private bool isConnected;
        public bool IsConnected
        {
            get
            {
                lock (locker)
                {
                    return isConnected;
                }
            }
            set
            {
                lock (locker)
                {
                    isConnected = value;
                }
            }
        }

        public bool Connected { get; set; }

        public ChatClient(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            Messages = new List<string>();
            OtherClients = new HashSet<string>();
            Client = new TcpClient();
        }

        public async Task StartClient()
        {
            await Client.ConnectAsync(Address, Port);
            IsConnected = true;
            var stream = Client.GetStream();
            Writer = new StreamWriter(stream);
            Writer.AutoFlush = true;

            using (var reader = new StreamReader(stream))
            {
                while (IsConnected)
                {
                    string response = await reader.ReadLineAsync();
                    var lines = response.Split(' ').ToList();
                    var command = lines[0].ToLowerInvariant();
                    if (command.Equals("/list"))
                    {
                        UpdateOtherClientNames(lines.GetRange(1, lines.Count - 1));
                    }
                    else if (command.Equals("/disconnect"))
                    {
                        IsConnected = false;
                    }
                    else
                    {
                        lock (Messages)
                        {
                            Messages.Add(string.Join(" ", lines.GetRange(1, lines.Count - 1)));
                        }
                    }
                }
            }
            Client.Close();
        }


        public async void SendMessage(string message)
        {
            await Writer.WriteLineAsync(message);
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
