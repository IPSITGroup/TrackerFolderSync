using ImageOptimizer;
using IPSPathUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextLogger;
using TimeUtilities;

namespace TrackerFolderSync6
{
    class Program
    {
        #region Declarations

        // ********** //
        // Resources  //
        // ********** //
        private static readonly EventSourceCreationData TrackerFolderSyncEventLog = new EventSourceCreationData("Tracker Folder Sync", "Application");

        // ********** //
        // Properties //
        // ********** //

        // Note program start time
        private static DateTime StartTime = DateTime.Now;

        // Flag for the type of jobs to be processed (active or inactive) [Active by default]
        private static bool ProcessActiveJobs = true;

        // Container to hold the queue of the jobs to be processed
        private static DataTable JobsToProcess = new DataTable();

        // Max size for optimized images
        private static int MaxOptimizedImageSize = 1000;

        // List of file types that are synced
        private static List<string> SyncedFileTypes = new List<string>() { ".jpg", ".bmp", ".gif", ".png", ".pdf" };

        // List of subdirectories that are synced
        private static List<string> SyncedDirectories = new List<string>() { "pics", "pictures", "test reports" };

        // ********** //
        // UI Fields  //
        // ********** //

        // Counters
        private static int Existing = 0;
        private static int Images = 0;
        private static int Documents = 0;
        private static int Unsynced = 0;
        private static int Removed = 0;
        private static int JobCounter = 0;
        private static int Errors = 0;
        private static int ErrorStartRow = 0;
        private static int ProgressStartRow = 0;

        #endregion Declarations
        static void Main(string[] args)
        {
            // Start up the logging mechanisms
            _initializeLogging();

            // Set the flag for active/inactive jobs based on the parameters passed
            foreach(string arg in args)
            {
                if (arg.Equals("-history"))
                    ProcessActiveJobs = false;
                else
                    _log("Warning", "Unrecognized parameter: \"" + arg + "\". Continuing with defaults.");
            }

            // Let the logs know the sync is starting
            _log("info", (ProcessActiveJobs ? "Active " : "Inactive ") + "job sync started " + StartTime, true);

            // Let the user know the sync is starting
            Console.WriteLine("Tracker Folder Sync - Version 1.2.0 - 12/12/2016");

            // Check to see if there are any jobs matching the critera that need to be processed
            if (JobProcessingNeeded())
            {
                Console.WriteLine();
                Console.WriteLine("Found " + JobsToProcess.Rows.Count + " jobs to be processed.");
                Console.WriteLine("Beginning data transfer from Schintranet to Schweb...");
            }
            else
                return;

            // begin processing
            ProgressStartRow = Console.CursorTop + 4;
            ErrorStartRow = ProgressStartRow + 10;

            foreach(DataRow Job in JobsToProcess.Rows)
            {
                // increment the job counter
                JobCounter++;

                string divNumber = Job[0].ToString();
                string jobNumber = Job[1].ToString();

                try {

                    // process the files
                    SyncJobDirectories(jobNumber, divNumber);
                }
                catch (Exception ex)
                {
                    // build error message
                    var ErrorMessage = $"Processing job fail. Job {jobNumber}. {ex.Message + (ex.InnerException == null ? "" : (string.IsNullOrWhiteSpace(ex.InnerException.Message) ? "" : $"({ex.InnerException.Message})"))}";

                    // let the logs know what happened
                    _log("error", ErrorMessage);

                    // increment errors count
                    Errors++;

                    // let the user know what happened
                    _reportError(ErrorMessage);
                }
            }

            _reportStatus(" - ", " - ", " - ", " - ", "completed");
            TimeSpan Runtime = DateTime.Now - StartTime;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Sync complete " + DateTime.Now + " | " + Runtime.ToReadableString());
            _log("info", "Sync complete " + DateTime.Now + " | " + Runtime.ToReadableString(), true);
            Console.ReadLine();
        }
        private static void SyncJobDirectories(string jobNumber, string divNumber)
        {
            // build resources
            string schintranetJobDirectory = PathHelpers.BuildSchintranetJobDirectoryPath(divNumber, jobNumber);
            string schwebJobDirectory = PathHelpers.BuildSchwebJobDirectoryPath(divNumber, jobNumber);

            // create schintranet job directory if it doesn't exist
            if (!Directory.Exists(schintranetJobDirectory))
            {
                Directory.CreateDirectory(schintranetJobDirectory);
                _log("info", "Job directory created on schintranet: " + jobNumber);
            }

            // create the job directory on schweb if it's not there
            if (!Directory.Exists(schwebJobDirectory))
            {
                Directory.CreateDirectory(schwebJobDirectory);
                _log("info", "Job directory created on schweb: " + jobNumber);
            }

            // build the approved file list to hold files that can remain on schweb
            List<string> ApprovedFiles = new List<string>();

            // log what's happening
            _log("info", "Syncing directory: " + schintranetJobDirectory.Replace(PathHelpers.SchintranetJobsDirectory, " ~ Schintranet "));

            // begin sync
            foreach(string file in Directory.GetFiles(schintranetJobDirectory, "*.*", SearchOption.AllDirectories))
            {
                // build the schweb file path
                string schwebFilePath = PathHelpers.BuildSchwebFilePath(divNumber, jobNumber, Path.GetFileName(file));

                // let the user know what's going on
                _reportStatus(jobNumber, divNumber, file, schwebFilePath, "scanning");

                // check if the file is in a subdirectory and if it's a synced directory
                // =====================================================================
                // NOTE: if we want to keep what's in the 't' directory on schintranet,
                // and copy the contents over to the job's root directory in schweb,
                // add 't' to the list of snyced directories.
                // =====================================================================
                if (!Path.GetDirectoryName(file).Equals(schintranetJobDirectory) &&
                    !SyncedDirectories.Contains(new FileInfo(file).Directory.Name))
                {
                    _log("info", "File in subdirectory skipped: " + file.Replace(PathHelpers.SchintranetJobsDirectory, " ~ Schintranet "));
                    Unsynced++;
                }
                // if it's a file type that's not synced, keep moving
                else if (!SyncedFileTypes.Contains(Path.GetExtension(file).ToLower()))
                    Unsynced++;
                else if (File.Exists(schwebFilePath))
                {
                    // if the file exists on schweb, make sure it's the most recent version
                    if (File.GetLastWriteTime(file).CompareTo(File.GetLastWriteTime(schwebFilePath)) > 0)
                    {
                        // let the user know what's happening
                        _reportStatus(jobNumber, divNumber, file, schwebFilePath, "copying");

                        // the schintranet version is more recent than the schweb version, so copy it
                        CopyToSchweb(file, schwebFilePath);
                    }
                    else
                    {
                        // file is up to date, keep rolling
                        Existing++;
                        _log("info", "File skipped. Up to date on Schweb: " + file.Replace(PathHelpers.SchintranetJobsDirectory, " ~ Schintranet "));
                    }

                    // add this file to the approved files list
                    ApprovedFiles.Add(schwebFilePath);
                }
                else
                {
                    // let the user know what's happening
                    _reportStatus(jobNumber, divNumber, file, schwebFilePath, "copying");

                    // file isn't on schweb, so copy it
                    CopyToSchweb(file, schwebFilePath);

                    // add this file to the approved schweb file list
                    ApprovedFiles.Add(schwebFilePath);
                }
            }

            // clean up files on schweb that shouldn't be there
            // -- WARNING: This part deletes files from schweb.
            // -- NEVER: Delete files from schintranet
            List<string> filesInJobDirectoryOnSchweb = Directory.GetFiles(schwebJobDirectory, "*.*", SearchOption.AllDirectories).ToList();
            IEnumerable<string> filesToDeleteFromSchweb = filesInJobDirectoryOnSchweb.Except(ApprovedFiles);
            _log("info", filesInJobDirectoryOnSchweb.Count + " files found in " + schwebJobDirectory.Replace(PathHelpers.SchwebJobsDirectory, " ~ Schweb ") + " | " + filesToDeleteFromSchweb.Count() + " to delete.");

            foreach (string file in filesToDeleteFromSchweb)
            {
                File.Delete(file);
                _reportStatus(jobNumber, divNumber, schwebFilePath: file, status: "deleting");
                _log("info", "File deleted from schweb: " + file.Replace(PathHelpers.SchwebJobsDirectory, " ~ Schweb "));
                Removed++;
            }
        }
        private static void CopyToSchweb(string file, string schwebFilePath)
        {
            if(ImageHelpers.IsImage(file))
            {
                ImageHelpers.OptimizeAndCopy(file, schwebFilePath, MaxOptimizedImageSize);
                _log("info", "Image copied from: " + file.Replace(PathHelpers.SchintranetJobsDirectory, " ~ Schintranet ") + " to " + schwebFilePath.Replace(PathHelpers.SchwebJobsDirectory, " ~ Schweb ") + ".");
                Images++;
            }
            else
            {
                File.Copy(file, schwebFilePath, true);
                _log("info", "Document copied from: " + file.Replace(PathHelpers.SchintranetJobsDirectory, " ~ Schintranet ") + " to " + schwebFilePath.Replace(PathHelpers.SchwebJobsDirectory, " ~ Schweb ") + ".");
                Documents++;
            }
        }
        private static bool JobProcessingNeeded()
        {
            try
            {
                // define the query to be executed against schsql01 for active jobs
                // or inactive jobs depending on the parameters passed
                string QueryString;
                if(ProcessActiveJobs)
                    QueryString = "SELECT DIV, J_JOB_NUMBER FROM JC_MASTER WHERE CUSTOMER_NBR IN (SELECT DISTINCT CustNbr FROM tracker..webuser) ORDER BY DIV, J_JOB_NUMBER ";
                else
                    QueryString = "SELECT DIV, J_JOB_NUMBER FROM JC_MASTER_H WHERE CUSTOMER_NBR IN (SELECT DISTINCT CustNbr FROM tracker..webuser) ORDER BY DIV, J_JOB_NUMBER ";

                // initialize schsql01 connection and query
                SqlConnection SchSql01 = new SqlConnection("server=schsql01;database=acs;Integrated Security=SSPI;");
                SqlCommand JobsQuery = new SqlCommand(QueryString, SchSql01);

                // open the connection
                SchSql01.Open();

                // execute the query and populate the data table with the jobs
                using (SqlDataAdapter Adapter = new SqlDataAdapter(JobsQuery))
                    Adapter.Fill(JobsToProcess);

                // we have the jobs, close the connection to schsql01
                JobsQuery.Dispose();
                SchSql01.Close();

                // if there are any jobs to be processed, let the caller know
                if (JobsToProcess.Rows.Count > 0)
                {
                    _log("info", JobsToProcess.Rows.Count + " jobs found to process.");
                    return true;
                }
                else
                {
                    _log("info", "Didn't find any jobs to process.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // build the error message
                string ErrorMessage = "Unable to read jobs from schsql01. " + ex.Message + (ex.InnerException == null ? "" : (string.IsNullOrEmpty(ex.InnerException.Message) ? "" : " (" + ex.InnerException.Message + ")"));

                // tell the logs what happened
                _log("fatal", ErrorMessage, true);

                // tell the user what happened
                Console.WriteLine(ErrorMessage);

                // die
                return false;
            }
        }
        private static void _initializeLogging()
        {
            // Set up the log in the event logger
            if (!EventLog.SourceExists(TrackerFolderSyncEventLog.Source))
                EventLog.CreateEventSource(TrackerFolderSyncEventLog);

            // Fire up the text log file
            Log.Init();
        }
        private static void _log(string messageType, string message, bool writeToEventLog = false)
        {
            // normalize the message type before processing it
            messageType = messageType.ToLower();

            // Hold the Event Entry type so we can differentiate it when
            // writing it to the Windows Event Log
            EventLogEntryType eventLogMessageType = EventLogEntryType.Information;

            // Write to log file
            switch (messageType)
            {
                case "info":
                    Log.Info(message);
                    break;
                case "warning":
                case "warn":
                    Log.Warning(message);
                    eventLogMessageType = EventLogEntryType.Warning;
                    break;
                case "error":
                    Log.Error(message);
                    eventLogMessageType = EventLogEntryType.Error;
                    break;
                case "fatal":
                    Log.Fatal(message);
                    eventLogMessageType = EventLogEntryType.Error;
                    break;
            }

            // Write to Windows Event Log if necessary
            if (writeToEventLog)
                EventLog.WriteEntry(TrackerFolderSyncEventLog.Source, message, eventLogMessageType);
        }
        private static void _reportStatus(string jobNumber, string divNumber, string schintranetFilePath = null, string schwebFilePath = null, string status = null)
        {
            Console.Title = "Tracker Folder Sync - " + JobCounter + " / " + JobsToProcess.Rows.Count + " - " + Math.Round((decimal)(JobCounter / JobsToProcess.Rows.Count) * 100, 2) + "%";
            Console.CursorTop = ProgressStartRow;
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.CursorLeft = 32;
            Console.Write("]");

            float tick = 30.0f / JobsToProcess.Rows.Count;

            // draw progress
            int position = 1;
            for(int i = 0; i < tick * JobCounter; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            // draw remaining
            for(int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            // write totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(JobCounter + " of " + JobsToProcess.Rows.Count + " jobs processed.");
            if (!string.IsNullOrEmpty(schintranetFilePath))
            {
                if (schintranetFilePath.Length > 60)
                    schintranetFilePath = schintranetFilePath.Substring(0, 30) + "..." + schintranetFilePath.Substring(schintranetFilePath.Length - 30);
                Console.WriteLine();
                Console.WriteLine("Current file: " + schintranetFilePath);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("{0:5}  |  {1:5}  |  {2:5}  ", "Current div: " + divNumber, "Current job: " + jobNumber, status ?? "");
            Console.WriteLine();
            Console.WriteLine("{0:5}  |  {1:5}  |  {2:5}  |  {3:5}  |  {4:5}  ", "Skipped: " + Existing, "Pics: " + Images, "Docs: " + Documents, "Unsynced: " + Unsynced, "Removed: " + Removed);
        }
        private static void _reportError(string message)
        {
            Console.CursorTop = ErrorStartRow;
            Console.WriteLine(message);
            ErrorStartRow++;
        }
    }
}
