using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    public class ChatConnection
    {
        public string Name { get; set; }

        public TcpClient Client { get; set; }

        public byte[] Buffer { get; set; }

        private static int BUFFERSIZE = 1024;

        public ChatConnection(string name, TcpClient client)
        {
            Name = name;
            Client = client;
            Buffer = new byte[BUFFERSIZE];
        }

        public string ReadBufferAndReset(int length)
        {
            var message = Encoding.UTF8.GetString(Buffer, 0, length).Trim();
            Array.Clear(Buffer, 0, length);
            return message;
        }

        public void BeginRead(AsyncCallback callback, object state)
        {
            if (Client.Connected)
                Client.GetStream().BeginRead(Buffer, 0, BUFFERSIZE, callback, state);
        }

        public void BeginWrite(AsyncCallback callback, object state, string message)
        {
            if (Client.Connected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                Client.GetStream().BeginWrite(buffer, 0, buffer.Length, callback, state);
            }
        }

        public void CloseConnection()
        {
            Name = null;
            Buffer = null;
            Client.Close();
        }


    }
}
