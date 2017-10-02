using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerFolderSync7.Properties;

namespace TrackerFolderSync7.Utilities
{
    public static class PathHelper
    {
        public static string BuildSchwebFilePath(string divNumber, string jobNumber, string fileNameWithExtension)
        {
            return Path.Combine(string.Format(Settings.Default.SchwebJobsPath, divNumber, jobNumber), fileNameWithExtension);
        }

        public static string BuildSchintranetJobDirectoryPath(string divNumber, string jobNumber)
        {
            return string.Format(Settings.Default.SchintranetJobsPath, divNumber, jobNumber);
        }

        public static string BuildSchwebJobDirectoryPath(string divNumber, string jobNumber)
        {
            return string.Format(Settings.Default.SchwebJobsPath, divNumber, jobNumber);
        }
    }
}
