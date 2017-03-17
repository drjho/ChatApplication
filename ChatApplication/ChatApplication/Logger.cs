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
        /// <summary>
        /// Want to write to the file directly in case the program crashes.
        /// </summary>
        private StreamWriter Writer { get; set; }

        /// <summary>
        /// Just in case for thread safety.
        /// </summary>
        private readonly object locker = new object();

        /// <summary>
        /// Create a logger with a default filename.
        /// </summary>
        public Logger() : this("log.txt")
        {

        }

        /// <summary>
        /// Create a logger with the provided filename.
        /// It will create a new file or write over the file.
        /// </summary>
        /// <param name="filename"></param>
        public Logger(string filename)
        {
            lock (locker)
            {
                Writer = File.CreateText(filename);
                Writer.AutoFlush = true;
            }
        }

        /// <summary>
        /// Log a message with a level
        /// </summary>
        /// <param name="level"></param>
        /// <param name="log"></param>
        public void Log(LogLevel level, string log)
        {
            var timeStr = DateTime.Now.ToString();
            lock (locker)
            {
                Writer.WriteLine($"{timeStr}:{level.ToString()}:{log}");
                Writer.Flush();
            }
        }

        /// <summary>
        /// Called upon when the server is shutting down.
        /// </summary>
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
