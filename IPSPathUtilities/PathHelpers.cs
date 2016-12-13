using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSPathUtilities
{
    public class PathHelpers
    {
        public static readonly string SchintranetJobsDirectory = @"\\161.153.138.46\acs\acs\firms\";
        public static readonly string SchwebJobsDirectory = @"\\192.168.1.15\Websites\IPS Tracker\private\firms";
        public static string BuildSchwebFilePath(string divNumber, string jobNumber, string fileNameWithExtension)
        {
            return Path.Combine(@"\\192.168.1.15\Websites\IPS Tracker\private\firms\", "firm" + divNumber, @"images\jobs", jobNumber, fileNameWithExtension);
        }

        public static string BuildSchintranetJobDirectoryPath(string divNumber, string jobNumber)
        {
            return Path.Combine(@"\\161.153.138.46\acs\acs\firms\", "firm" + divNumber, @"images\jobs\", jobNumber);
        }

        public static string BuildSchwebJobDirectoryPath(string divNumber, string jobNumber)
        {
            return Path.Combine(@"\\192.168.1.15\Websites\IPS Tracker\private\firms\", "firm" + divNumber, @"images\jobs\", jobNumber);
        }
    }
}
