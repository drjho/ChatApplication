using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    class ChatConnection
    {
        public string Name { get; set; }

        public TcpClient Client { get; set; }

        public StreamWriter Writer { get; set; }

        public StreamReader Reader { get; set; }

        public ChatConnection(string name, TcpClient client)
        {
            Name = name;
            Client = client;
            Writer = new StreamWriter(client.GetStream());
            Writer.AutoFlush = true;
            Reader = new StreamReader(client.GetStream());
        }

        public async Task<string> ReadMessageAsync()
        {
            return await Reader.ReadLineAsync();
        }

        public async Task WriteMessageAsync(string message)
        {
            await Writer.WriteLineAsync(message);
        }

        public void CloseConnection()
        {
            Client.Close();
        }
 

    }
}
