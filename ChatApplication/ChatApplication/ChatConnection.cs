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

        public StreamReader Reader { get; set; }

        public StreamWriter Writer { get; set; }

        public ChatConnection(string name, TcpClient client)
        {
            Name = name;
            Client = client;
            NetworkStream stream = client.GetStream();
            Reader = new StreamReader(stream);
            Writer = new StreamWriter(stream);
            Writer.AutoFlush = true;
        }

        public async Task<string> ReadLineAsync()
        {
            return await Reader.ReadLineAsync();
        }

        public async void WriteLineAsync(string message)
        {
            await Writer.WriteLineAsync(message);
        }

        public void CloseConnection()
        {
            Client.Close();
        }
 

    }
}
