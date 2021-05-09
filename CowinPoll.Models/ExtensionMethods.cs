using System;
using System.Collections.Generic;
using System.Text;

namespace CowinPoll.Models
{
    public static class ExtensionMethods
    {
        public static string GenerateResponseMessage(this Appointment appointmentData, string code)
        {
            var sb = new StringBuilder();
            if (appointmentData == null || appointmentData.Centers == null || appointmentData.Centers.Length == 0)
                sb.AppendLine($"*No appointment data available for  {code}*");
            foreach (var center in appointmentData.Centers)
            {
                sb.AppendLine($"*Name*:{center.Name}, *Paid*:{center.FeeType}");
                foreach (var session in center.Sessions)
                {
                    sb.AppendLine($"- *Date*:{session.Date}, *Available*:{session.AvailableCapacity}, *Age*: {session.MinAgeLimit}");
                }
                sb.AppendLine(" ");
            }

            return sb.ToString();
        }
    }
}
