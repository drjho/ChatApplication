﻿using ChatApplication;
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
            Task.Run(() => chatServer.StartServer(13000));

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Update server messages by pressing [Enter]");
                Console.WriteLine("ServerMessage:");
                foreach (var message in chatServer.Messages)
                {
                    Console.WriteLine(message);
                }
                Console.WriteLine("----------");
                Console.Write("> ");
                if (Console.ReadLine().ToLowerInvariant().Equals("/exit"))
                {
                    chatServer.IsShuttingDown = true;
                    break;
                }
            }
            Console.WriteLine("Server is shut down");
        }
    }
}