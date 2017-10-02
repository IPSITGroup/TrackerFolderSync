using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TrackerFolderSync7.Properties;

namespace TrackerFolderSync7.Utilities
{
    public class Log
    {
        private static readonly string LogFilePath = Environment.ExpandEnvironmentVariables(Settings.Default.LogFilePath);

        private static readonly EventSourceCreationData TrackerEventLog = new EventSourceCreationData(Assembly.GetExecutingAssembly().GetName().Name, "Application");

        public static void Debug(string message)
        {
            Write("DEBUG", message);
        }

        public static void Information(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARNING", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        public static void Fatal(string message, Exception ex = null)
        {
            Write("FATAL", message, ex);
            EventLog.WriteEntry(TrackerEventLog.Source, $"{message}. Check the log file at {LogFilePath} for more information.", EventLogEntryType.Error);
        }

        private static void Write(string logLevel, string message, Exception ex = null)
        {
            using (var writer = new StreamWriter(LogFilePath))
                writer.WriteLine("[{0:5}] {1:5} {2:20} {3:50}", DateTime.Now.ToString(), logLevel, message, $"{ex?.Message} ({ex?.InnerException?.Message})");
        }

        public static void CreateLogger()
        {
            var logDirectory = new FileInfo(LogFilePath).DirectoryName;

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            using (var writer = new StreamWriter(LogFilePath))
            {
                writer.WriteLine();
                writer.WriteLine("-------------------------------------------------------------------");
                writer.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}");
                writer.WriteLine($"Application startup - {DateTime.Now}");
                writer.WriteLine("-------------------------------------------------------------------");
                writer.WriteLine();
            }

            if (!EventLog.SourceExists(TrackerEventLog.Source))
                EventLog.CreateEventSource(TrackerEventLog);
        }

        public static void CloseLog(string runtime)
        {
            using (var writer = new StreamWriter(LogFilePath))
            {
                writer.WriteLine("-------------------------------------------------------------------");
                writer.WriteLine($"Sync complete - {DateTime.Now}");
                writer.WriteLine("-------------------------------------------------------------------");
                writer.WriteLine();
            }

            EventLog.WriteEntry(TrackerEventLog.Source, $"IPS Tracker Jobs Sync completed in {runtime}. Log file located at {LogFilePath}.", EventLogEntryType.Information);
        }
    }
}
