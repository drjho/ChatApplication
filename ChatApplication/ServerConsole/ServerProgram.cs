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
            // Instantiate a new chat server.
            var chatServer = new ChatServer();

            // Start the server on port 13000.
            chatServer.StartServer(13000);

            // Clear the screen.
            Console.Clear();

            // Show possible event messages.
            chatServer.UpdateView();

            // Loop to receive admin inputs (commands).
            while (true)
            {
                Console.Write("> ");
                chatServer.ParseInput(Console.ReadLine());
                Console.Clear();
                chatServer.UpdateView();
                Console.SetCursorPosition(0, 0);

                // If the server is not running, then break this loop.
                if (!chatServer.Running)
                    break;
            }

            // Final message to the admin.
            Console.WriteLine("Server is shut down");
        }
    }
}
