using System;
using System.Collections.Generic;
using System.Text;

namespace CowinPoll.Models
{
    public class AppSettings
    {
        public int IntervalMinutes { get; set; }
        public long StartChatId { get; set; }
        public int LatestOffsetId { get; set; }
        public string[] SearchPincode { get; set; }
        public string[] SearchDistrict { get; set; }
        public int MaxDistrict { get; set; }
        public int MaxPincode { get; set; }

    }
}
