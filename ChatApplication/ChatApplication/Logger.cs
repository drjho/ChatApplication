using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    public enum LogLevel
    {
        Info, Warning, Error
    }


    class Logger
    {
        private StreamWriter Writer { get; set; }

        private readonly object locker = new object();


        public Logger() : this("log.txt")
        {

        }

        public Logger(string filename)
        {
            lock (locker)
            {
                Writer = File.CreateText(filename);
                Writer.AutoFlush = true;
            }
        }

        public void Log(LogLevel level, string log)
        {
            var timeStr = DateTime.Now.ToString();
            lock (locker)
            {
                Writer.WriteLine($"{timeStr}:{level.ToString()}:{log}");
                Writer.Flush();
            }
        }

        public void CloseLog()
        {
            Log(LogLevel.Info, "server closing.");
            lock (locker)
            {
                Writer.Close();
            }
        }

    }
}
