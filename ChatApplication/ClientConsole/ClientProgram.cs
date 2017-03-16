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
            var name = AskForName();
            ChatClient chatClient = null;
            do
            {
                Console.WriteLine("Please Enter IP address and port (x.x.x.x:p)");
                var connectionString = Console.ReadLine();
                chatClient = TryCreateClientWithConnectionString(connectionString, name);
            } while (chatClient == null);

            Console.Clear();

            chatClient.UpdateView();

            while (true)
            {
                Console.Write("> ");
                chatClient.ParseInput(Console.ReadLine());
                Console.Clear();
                //chatClient.UpdateView();
                Console.SetCursorPosition(0, 0);

                if (!chatClient.Connected)
                    break;
            }

            Console.WriteLine("Bye!");
        }

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
