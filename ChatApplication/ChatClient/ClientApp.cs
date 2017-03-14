﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    class ClientApp
    {
        static void Main(string[] args)
        {
            while (true)
            {
                ChatClient chatClient = null;
                do
                {
                    Console.WriteLine("Please Enter IP address and port (x.x.x.x:p)");
                    var connectionString = Console.ReadLine();
                    chatClient = TryCreateClientWithConnectionString(connectionString);
                } while (chatClient == null);

                if (chatClient != null)
                    Task.Run(() => chatClient.StartClient());

                while (true)
                {
                    Console.Clear();
                    if (!chatClient.IsConnected)
                        break;
                    Console.WriteLine("Update chatter list by pressing [Enter]");
                    Console.WriteLine("Chatters:");
                    foreach (var name in chatClient.GetOtherClientNames())
                    {
                        Console.WriteLine(name);
                    }
                    Console.WriteLine("----------");
                    Console.WriteLine("ServerMessage:");
                    foreach (var message in chatClient.GetLastTenMessages())
                    {
                        Console.WriteLine(message);
                    }
                    Console.WriteLine("----------");
                    Console.Write("> ");
                    chatClient.SendMessage(Console.ReadLine());
                }
            }
        }

        static ChatClient TryCreateClientWithConnectionString(string connectionString)
        {
            var lines = connectionString.Split(':');
            IPAddress address;
            int port;
            if (IPAddress.TryParse(lines[0], out address) && int.TryParse(lines[1], out port))
            {
                return new ChatClient(address, port);
            }
            return null;
        }


    }
}