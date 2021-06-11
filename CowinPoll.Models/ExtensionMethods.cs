using System;
using System.Collections.Generic;
using System.Text;

namespace CowinPoll.Models
{
    public static class ExtensionMethods
    {
        public static string GenerateResponseMessage(this Appointment appointmentData)
        {
            var sb = new StringBuilder();
            if (appointmentData == null || appointmentData.centers == null || appointmentData.centers.Length == 0)
                return null;
            foreach (var center in appointmentData.centers)
            {
                sb.AppendLine($"*Name*:{center.name},{center.district_name},{center.pincode},{center.fee_type}");
                foreach (var session in center.sessions)
                {
                    sb.AppendLine($"- *Date*:{session.date},*Available*:{session.available_capacity},*Age*: {session.min_age_limit}");
                }
                sb.AppendLine(" ");
            }

            return sb.ToString();
        }
    }
}
