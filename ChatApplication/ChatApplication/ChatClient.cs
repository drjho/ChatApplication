using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    class ChatClient
    {
        private NetworkStream stream;

        public async Task StartClient(string ip, int port)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ip), port);
            stream = client.GetStream();
        }
    }
}
