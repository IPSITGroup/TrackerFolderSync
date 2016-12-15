# TrackerFolderSync
Syncs documents/images from Schintranet to Schweb. 
* Supported file types are ONLY .jpg, .png, .bmp, .gif, and .pdf. All other files will be ignored.
* By default, any files in subdirectories on schintranet are ignored with the exception of the following accepted subdirectory names: "Pictures", "Pics", or "test reports".
* The Application Event Log is written to when the application starts, when it completes, and in the event of any errors.
* A detailed log file is also generated at %appdata%\TrackerFolderSync\LogFile.log
* Images are automatically scaled down to a max width OR height of 1000px while keeping the same aspect ratio.

## Application workflow
1. Depending on whether or not the "-history" flag was passed, schweb is polled for job numbers and div numbers.
2. If no jobs are returned that match the query, the program writes to the event log and ends.
3. Iterates through each job returned and checks the job's directory on schintranet for any files.
4. Iterates through each file in the job directory on schintranet.
5. Builds the expected schweb path of each file (schintranet job directory + file name -- removes anything in approved subdirectories)
6. If the file exists on schweb, the last write times are compared to make sure schweb has the latest version.
7. If the version on schintranet (the source) is newer than the version on schweb, the file will be copied to schweb.
8. Else, the file will stay where it is.
9. If the file isn't on scweb, it will be copied.
10. Any files that are synced on schintranet are added to a list of "approved files". Once a directory has had its files copied to schweb, the job directory in schweb will be checked for any files that aren't in the list of "approved files". The files that are not WILL BE REMOVED from schweb.
