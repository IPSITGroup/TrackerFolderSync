using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerFolderSync7.Utilities
{
    public static class TimeHelper
    {
        public static string ToReadableString(this TimeSpan timeSpan)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                timeSpan.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", timeSpan.Days, timeSpan.Days == 1 ? String.Empty : "s") : string.Empty,
                timeSpan.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", timeSpan.Hours, timeSpan.Hours == 1 ? String.Empty : "s") : string.Empty,
                timeSpan.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", timeSpan.Minutes, timeSpan.Minutes == 1 ? String.Empty : "s") : string.Empty,
                timeSpan.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", timeSpan.Seconds, timeSpan.Seconds == 1 ? String.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }
    }
}
