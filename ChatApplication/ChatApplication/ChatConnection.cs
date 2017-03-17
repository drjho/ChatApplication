using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    /// <summary>
    /// A Class for storing the tcpClient and handle stream read/ write 
    /// </summary>
    public class ChatConnection : IEquatable<ChatConnection>
    {
        public string EndPoint { get; set; }

        public string UserName { get; set; }

        public TcpClient Client { get; set; }

        public byte[] Buffer { get; set; }

        private static int BUFFERSIZE = 1024;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client"></param>
        public ChatConnection(TcpClient client)
        {
            Client = client;
            Buffer = new byte[BUFFERSIZE];
        }

        /// <summary>
        /// Read the buffer and clear the content.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string ReadBufferAndReset(int length)
        {
            var message = Encoding.UTF8.GetString(Buffer, 0, length).Trim();
            Array.Clear(Buffer, 0, length);
            return message;
        }

        /// <summary>
        /// Set the stream to listen mode. 
        /// Both Server and Client can use this to redirect to their own ReadCallback.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public void BeginRead(AsyncCallback callback, object state)
        {
            if (Client.Connected)
                Client.GetStream().BeginRead(Buffer, 0, BUFFERSIZE, callback, state);
        }

        /// <summary>
        /// Send the message thru the stream,
        /// both Server and Client can use this to redirect to their own WriteCallback.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="message"></param>
        public void BeginWrite(AsyncCallback callback, object state, string message)
        {
            if (Client.Connected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                Client.GetStream().BeginWrite(buffer, 0, buffer.Length, callback, state);
            }
        }

        /// <summary>
        /// Close the tcpClient connection and underlying connection as well.
        /// </summary>
        public void CloseConnection()
        {
            Client.Close();
        }

        /// <summary>
        /// Method for IEquatable, used by Generic List to check whether two instances are equal.
        /// We use the localEndPoint for identification.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ChatConnection other)
        {
            if (other == null)
                return false;

            return (this.EndPoint == other.EndPoint);
        }
    }
}
