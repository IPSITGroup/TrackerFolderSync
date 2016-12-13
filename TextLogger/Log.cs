using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextLogger
{
    public class Log
    {
        static bool logInitialized;
        private static readonly string logFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Tracker Folder Sync\LogFile.log";
        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Fatal(string message)
        {
            // Only things that crash the application
            Write("FATAL", message);
        }

        private static void Write(string type, string message)
        {
            using (StreamWriter logFile = new StreamWriter(logFilePath, true))
            {
                logFile.WriteLine(string.Format("[{0:5}] {1:5} {2:20}", DateTime.Now.ToString(), type, message));
            }
        }

        public static void Init()
        {
            string logDirectory = new DirectoryInfo(logFilePath).Name;
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
            using (StreamWriter logFile = new StreamWriter(logFilePath))
            {
                logFile.WriteLine("Tracker Folder Sync version 1.2.0");
                logFile.WriteLine("Log File - " + DateTime.Now);
                logFile.WriteLine("--------------------------------------------------");
                logFile.WriteLine();
            }
        }
    }
}
