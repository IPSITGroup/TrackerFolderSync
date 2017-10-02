using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerFolderSync7.Utilities
{
    public static class ConsoleManager
    {
        public static int Existing { get; set; }

        public static int Images { get; set; }

        public static int Documents { get; set; }

        public static int Unsynced { get; set; }

        public static int Removed { get; set; }

        public static int JobCounter { get; set; }

        public static int TotalJobs { get; set; }

        public static int Errors { get; set; }

        public static int ErrorStartRow { get; set; }

        public static int ProgressStartRow { get; set; }

        public static void UpdateStatus(string jobNumber, string divNumber, string schintranetFilePath = null, string status = null)
        {
            Console.Title = $"Tracker Folder Sync - {JobCounter} / {TotalJobs} - {Math.Round(Decimal.Divide(JobCounter, TotalJobs) * 100, 2)}%";
            Console.CursorTop = ProgressStartRow;
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.CursorLeft = 32;
            Console.Write("]");

            var tick = 30.0f / TotalJobs;

            // Draw progress
            var position = 1;
            for (int i = 0; i < tick * JobCounter; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            // Draw remaining
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            // Write totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"{JobCounter} of {TotalJobs} jobs processed.");

            if (!string.IsNullOrWhiteSpace(schintranetFilePath))
            {
                if (schintranetFilePath.Length > 60)
                    schintranetFilePath = $"{schintranetFilePath.Substring(0, 30)}...{schintranetFilePath.Substring(schintranetFilePath.Length - 30)}";
                Console.WriteLine();
                Console.WriteLine($"ProcessingFile file: {schintranetFilePath}");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("{0:5}  |  {1:5}  |  {2:5}  ", $"Current div: {divNumber}", $"Current job: {jobNumber}", status ?? string.Empty);
            Console.WriteLine("{0:5}  |  {1:5}  |  {2:5}  |  {3:5}  |  {4:5}  ", $"Skipped: {Existing}", $"Pics: {Images}", $"Docs: {Documents}", $"Unsynced: {Unsynced}", $"Removed: {Removed}");
        }

        public static void ReportError(Exception ex)
        {
            Console.CursorTop = ErrorStartRow;
            Console.WriteLine($"[{ex.Message}] {ex.TargetSite}");
            ErrorStartRow++;
        }
    }
}
