using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    public class ChatConnection : IEquatable<ChatConnection>
    {
        public string EndPoint { get; set; }

        public string UserName { get; set; }

        public TcpClient Client { get; set; }

        public byte[] Buffer { get; set; }

        private static int BUFFERSIZE = 1024;

        public ChatConnection(TcpClient client)
        {
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
            Client.Close();
        }

        public bool Equals(ChatConnection other)
        {
            if (other == null)
                return false;

            return (this.EndPoint == other.EndPoint);
        }
    }
}
