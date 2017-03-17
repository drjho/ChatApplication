using ChatApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class ClientProgram
    {
        static void Main(string[] args)
        {
            // Ask for user name first.
            var name = AskForName();

            // The while-loop is not very effective right now,
            // it was supposed to be used for reconnecting to a server. 
            ChatClient chatClient = null;
            do
            {
                Console.WriteLine("Please Enter IP address and port (x.x.x.x:p)");
                var connectionString = Console.ReadLine();
                chatClient = TryCreateClientWithConnectionString(connectionString, name);
            } while (chatClient == null);

            // Clear the screen for view update.
            Console.Clear();

            // Show the list of other connected users and received messages.
            chatClient.UpdateView();

            // Loop for user inputs (commands).
            while (true)
            {
                //Console.Write("> ");
                chatClient.ParseInput(Console.ReadLine());
                Console.Clear();
                //chatClient.UpdateView();
                Console.SetCursorPosition(0, 0);

                // If the client (user) is not connected then break this loop.
                if (!chatClient.Connected)
                    break;
            }

            // Final message to the user.
            Console.WriteLine("Bye!");
        }

        /// <summary>
        /// Check if the connectionString is correct and return a new instance of chat client.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static ChatClient TryCreateClientWithConnectionString(string connectionString, string name)
        {
            var lines = connectionString.Split(':');
            if (lines.Length != 2)
                return null;
            IPAddress address;
            int port = 0;
            if (!IPAddress.TryParse(lines[0], out address))
            {
                Console.WriteLine("Error in the IPAddress: " + lines[0]);
                return null;
            }
            if (!int.TryParse(lines[1], out port))
            {
                Console.WriteLine("Error with Port: " + lines[1]);
                return null;
            }
            return new ChatClient(address, port, name);
        }

        /// <summary>
        /// The user has to provide a name.
        /// </summary>
        /// <returns></returns>
        static string AskForName()
        {
            string name = "";
            while (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                Console.Clear();
                Console.WriteLine("Please enter your name: ");
                name = Console.ReadLine();
            }
            return name;
        }
    }
}
