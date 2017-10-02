using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using TrackerFolderSync7.Properties;
using TrackerFolderSync7.Utilities;

namespace TrackerFolderSync7
{
    class Program
    {
        private static DateTime StartTime;

        private static bool ProcessActiveJobs = true;

        private static readonly DataTable JobsToProcess = new DataTable();

        static void Main(string[] args)
        {
            InitializeLogging();

            // Set the flag for active/inactive jobs based on the parameters passed
            foreach (var arg in args)
                if (arg.Equals("-history"))
                    ProcessActiveJobs = false;
                else
                    Log.Warning($"Unrecognized parameter: \"{arg}\". Continuing with default options.");

            // Log the startup
            StartTime = DateTime.Now;
            Log.Information($"{(ProcessActiveJobs ? "Active " : "Inactive ")} job sync started at {StartTime}.");

            // Write it out to the console
            Console.WriteLine("Tracker Folder Sync - Version 1.3.0 - 9/20/2017");

            // Check to see if any jobs need processing
            // If there aren't, die.
            if (!thereAreJobsToBeProcessed())
            {
                Log.Information("No jobs found to process.");
                Console.WriteLine("No jobs found to process.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Found {JobsToProcess.Rows.Count} jobs to process.");
            Console.WriteLine("Beginning data transfer from SCHINTRANET to SCHWEB...");

            // Start processing
            ConsoleManager.ProgressStartRow = Console.CursorTop + 4;
            ConsoleManager.ErrorStartRow = ConsoleManager.ProgressStartRow + 10;

            foreach(DataRow Job in JobsToProcess.Rows)
            {
                // Increment job count
                ConsoleManager.JobCounter++;

                var divNumber = Job[0].ToString();
                var jobNumber = Job[1].ToString();

                // Process the files
                SyncJobDirectories(jobNumber, divNumber);
            }

            ConsoleManager.UpdateStatus(" - ", " - ", " - ", " - ");
            var runtime = DateTime.Now - StartTime;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Sync completed {DateTime.Now} | {runtime.ToReadableString()} runtime");
            Log.Information($"Sync completed {DateTime.Now} | {runtime.ToReadableString()} runtime.");

            Console.ReadLine();
        }

        private static void SyncJobDirectories(string jobNumber, string divNumber)
        {
            // Build paths
            var schintranetJobDirectory = PathHelper.BuildSchintranetJobDirectoryPath(divNumber, jobNumber);
            var schwebJobDirectory = PathHelper.BuildSchwebJobDirectoryPath(divNumber, jobNumber);

            // Create schintranet directory if it doesn't exist
            if(!Directory.Exists(schintranetJobDirectory))
            {
                Directory.CreateDirectory(schintranetJobDirectory);
                Log.Debug($"Job directory created on Schintranet: {schintranetJobDirectory.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}.");
            }

            // Create schweb directory if it doesn't exist
            if(!Directory.Exists(schwebJobDirectory))
            {
                Directory.CreateDirectory(schwebJobDirectory);
                Log.Debug($"Job directory created on Schweb: {schwebJobDirectory.Replace(Settings.Default.SchwebJobsDirectory, "~Schweb")}.");
            }

            // Build the list of approved files that can stay on schweb
            var ApprovedFiles = new List<string>();

            // Log what's going on
            Log.Debug($"Syncing directory: {schintranetJobDirectory.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}");

            // Begin the sync
            foreach(var file in Directory.GetFiles(schintranetJobDirectory, "*.*", SearchOption.AllDirectories))
            {
                // Create the schweb file path
                var schwebFilePath = PathHelper.BuildSchwebFilePath(divNumber, jobNumber, Path.GetFileName(file));

                // Report the status
                ConsoleManager.UpdateStatus(jobNumber, divNumber, schwebFilePath, status: "scanning");

                // Check if the file is in a subdirectory and if that directory is a synced directory
                if(!Path.GetDirectoryName(file).Equals(schintranetJobDirectory)
                    && !Settings.Default.SyncedDirectories.Split(',').Contains(new FileInfo(file).Directory.Name))
                {
                    Log.Debug($"File in subdirectory skipped: {file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}");
                    ConsoleManager.Unsynced++;
                }
                // If it's a file type that's not synced, keep moving
                else if(!Settings.Default.SyncedFileTypes.Split(',').Contains(Path.GetExtension(file).ToLower()))
                {
                    Log.Debug($"Unsupported file type skipped ({Path.GetExtension(file).ToLower()}): {file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}");
                    ConsoleManager.Unsynced++;
                }
                else if (File.Exists(schwebFilePath))
                {
                    // If the file exists on schweb, make sure it's the most recent version
                    if(File.GetLastWriteTime(file).CompareTo(File.GetLastWriteTime(schwebFilePath)) > 0)
                    {
                        ConsoleManager.UpdateStatus(jobNumber, divNumber, file, "copying");

                        // Copy the file to Schweb since it's newer
                        CopyToSchweb(file, schwebFilePath);
                    }
                    else
                    {
                        // File is up to date, continue.
                        ConsoleManager.Existing++;
                        Log.Debug($"{file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")} is up to date on Schweb. Continuing.");
                    }
                    ApprovedFiles.Add(schwebFilePath);
                }
                else
                {
                    ConsoleManager.UpdateStatus(jobNumber, divNumber, file, "copying");
                    CopyToSchweb(file, schwebFilePath);
                    ApprovedFiles.Add(schwebFilePath);
                }
            }

            // Clean up files on schweb that don't have a counterpart on Schintranet
            // -- WARNING: Files will be deleted from Schweb here
            // -- DO NOT DELETE FILES FROM SCHINTRANET
            var filesInJobDirectoryOnSchweb = Directory.GetFiles(schwebJobDirectory, "*.*", SearchOption.AllDirectories);
            var filesToDeleteFromSchweb = filesInJobDirectoryOnSchweb.Except(ApprovedFiles);

            Log.Debug($"{filesToDeleteFromSchweb.Count()} files found in {schwebJobDirectory.Replace(Settings.Default.SchwebJobsDirectory, "~Schweb")} | {filesToDeleteFromSchweb.Count()} will be deleted.");

            foreach (var file in filesToDeleteFromSchweb)
            {
                try
                {
                    ConsoleManager.UpdateStatus(jobNumber, divNumber, file, "deleting");
                    File.Delete(file);
                    Log.Debug($"{file.Replace(Settings.Default.SchwebJobsDirectory, "~Schweb")} successfully deleted.");
                    ConsoleManager.Removed++;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to delete file {file.Replace(Settings.Default.SchwebJobsDirectory, "~Schweb")}.");
                    ConsoleManager.ReportError(ex);
                }
            }
        }

        private static void CopyToSchweb(string file, string schwebFilePath)
        {
            if(ImageHelper.IsImageFile(file))
            {
                try
                {
                    ImageHelper.OptimizeAndCopy(file, schwebFilePath, Settings.Default.MaxOptimizedImageSize);
                    Log.Debug($"Image successfully copied from {file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")} to {schwebFilePath.Replace(Settings.Default.SchwebJobsDirectory, "~Schweb")}.");
                    ConsoleManager.Images++;
                }
                catch(Exception ex)
                {
                    Log.Error(ex, $"Failed to copy image ({file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}) to schweb.");
                    ConsoleManager.Errors++;
                    ConsoleManager.ReportError(ex);
                }
            }
            else
            {
                try
                {
                    File.Copy(file, schwebFilePath, true);
                    Log.Debug($"Document successfully copied from {file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")} to {schwebFilePath.Replace(Settings.Default.SchwebJobsDirectory, "~Schweb")}.");
                    ConsoleManager.Documents++;
                }
                catch(Exception ex)
                {
                    Log.Error(ex, $"Failed to copy doucment ({file.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}) to schweb.");
                    ConsoleManager.Errors++;
                    ConsoleManager.ReportError(ex);
                }
            }
        }

        private static bool thereAreJobsToBeProcessed()
        {
            // define the query to be executed against schsql01 for the jobs
            var queryString = string.Empty;
            if (ProcessActiveJobs)
                queryString = "SELECT DIV, J_JOB_NUMBER FROM JC_MASTER WHERE CUSTOMER_NBR IN (SELECT DISTINCT CustNbr FROM tracker..webuser) ORDER BY DIV, J_JOB_NUMBER ";
            else
                queryString = "SELECT DIV, J_JOB_NUMBER FROM JC_MASTER_H WHERE CUSTOMER_NBR IN (SELECT DISTINCT CustNbr FROM tracker..webuser) ORDER BY DIV, J_JOB_NUMBER ";

            try
            {
                // initialize schsql01 connection and query
                using (var SchSql01 = new SqlConnection("server=schsql01;database=acs;Integrated Security=SSPI"))
                using (var JobsQuery = new SqlCommand(queryString, SchSql01))
                using (var Adapter = new SqlDataAdapter(JobsQuery))
                {
                    // Open the connection
                    SchSql01.Open();

                    // Log the connection
                    Log.Debug("Connection to SCHSQL01 initialized successfully.");

                    try
                    {
                        // Execute the query and populate the datatable with it
                        Adapter.Fill(JobsToProcess);

                        // If there are any jobs to be processed, let the caller know
                        if(JobsToProcess.Rows.Count <= 0)
                        {
                            Log.Information("No jobs found to process.");
                            return false;
                        }

                        // Set the total number to the total number of rows
                        ConsoleManager.TotalJobs = JobsToProcess.Rows.Count;

                        Log.Information($"{JobsToProcess.Rows.Count} jobs found to process.");
                        return true;
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex, "Error retreiving data from SCHSQL01.");
                        ConsoleManager.ReportError(ex);
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to connect to SCHSQL01.");
                ConsoleManager.ReportError(ex);
                return false;
            }
        }

        private static void InitializeLogging()
        {
            // Set up the log in the event logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Environment.ExpandEnvironmentVariables(Settings.Default.LogFilePath))
                .WriteTo.EventLog(Settings.Default.EventSource)
                .CreateLogger();

            // Register global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Fatal((Exception)e.ExceptionObject, "Unhandled application error.");
        }
    }
}
