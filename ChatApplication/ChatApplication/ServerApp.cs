using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    class ServerApp
    {
        static void Main(string[] args)
        {
            var chatServer = new ChatServer();
            Task.Run(() => chatServer.StartServer(13000));
        }
    }
}
