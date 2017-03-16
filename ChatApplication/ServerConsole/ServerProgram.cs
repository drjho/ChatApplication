using ChatApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerConsole
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            var chatServer = new ChatServer();
            chatServer.StartServer(13000);

            Console.Clear();

            Task.Run(() => chatServer.UpdateView());

            while (true)
            {
                Console.Write("> ");
                chatServer.ParseInput(Console.ReadLine());
                Console.Clear();
                chatServer.UpdateView();
                Console.SetCursorPosition(0, 0);

                if (!chatServer.Running)
                    break;
            }

            Console.WriteLine("Server is shut down");
        }
    }
}
