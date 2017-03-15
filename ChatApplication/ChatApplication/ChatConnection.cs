using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class ChatConnection
    {
        public string Name { get; set; }

        public TcpClient Client { get; set; }

        public StreamWriter Writer { get; set; }

        public ChatConnection(string name, TcpClient client)
        {
            Name = name;
            Client = client;
            Writer = new StreamWriter(client.GetStream());
            Writer.AutoFlush = true;
        }

        public async Task WriteLineAsync(string message)
        {
            await Writer.WriteLineAsync(message);
        }

        public void CloseConnection()
        {
            Client.Close();
        }
 

    }
}
