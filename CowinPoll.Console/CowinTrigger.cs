using CowinPoll.Models;
using CowinPoll.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CowinPoll.Server
{
    public class CowinTrigger
    {
        public static Dictionary<string, string> GetCowinResponses(string[] pincodes, string[] districts)
        {
            Response<Appointment> appointmentResponse;
            var date = DateTime.Now.ToString("dd-MM-yyyy");
            var responses = new Dictionary<string, string>();
            foreach (var pin in pincodes)
            {
                if (responses.ContainsKey(pin)) continue;
                appointmentResponse = new CowinService().GetAppointmentByPin(pin, date);
                if (appointmentResponse.Success)
                {
                    var availableCenters = appointmentResponse.Content.centers.ToList().Where(q => q.sessions.Any(q => q.available_capacity > 0 && q.min_age_limit == 18)).ToList();
                    foreach (var center in availableCenters)
                    {
                        center.sessions = center.sessions.Where(q => q.available_capacity > 0 && q.min_age_limit == 18).ToArray();
                    }
                    var responseText = (new Appointment() { centers = availableCenters.ToArray() }).GenerateResponseMessage();
                    if (!string.IsNullOrEmpty(responseText))
                        responses.Add(pin, responseText);
                }
            }

            foreach (var dist in districts)
            {
                if (responses.ContainsKey(dist)) continue;
                appointmentResponse = new CowinService().GetAppointmentByDistrict(dist, date);
                if (appointmentResponse.Success)
                {
                    var availableCenters = appointmentResponse.Content.centers.ToList().Where(q => q.sessions.Any(q => q.available_capacity_dose1>0 && q.available_capacity > 0 && q.min_age_limit == 18)).ToList();
                    foreach (var center in availableCenters)
                    {
                        center.sessions = center.sessions.Where(q => q.available_capacity_dose1>0 && q.available_capacity > 0 && q.min_age_limit == 18).ToArray();
                    }
                    var responseText = (new Appointment() { centers = availableCenters.ToArray() }).GenerateResponseMessage();
                    if (!string.IsNullOrEmpty(responseText))
                        responses.Add(dist, responseText);
                }
            }

            return responses;
        }

        public static IEnumerable<string> GetCowinResponsesFor21Days(string[] pincodes, string[] districts)
        {
            var responses = new List<string>();
            if (pincodes.Any())
            {
                var appointsByPin = new CowinService().GetAppointmentByPinFor21Days(pincodes.Distinct());
                if (appointsByPin.Success)
                {
                    var responsePin = AppointmentsToText(appointsByPin);
                    responses.AddRange(responsePin);
                }
            }

            if (districts.Any())
            {
                var appointsByDistrict = new CowinService().GetAppointmentByDistrictFor21Days(districts.Distinct());
                if (appointsByDistrict.Success)
                {
                    var responseDist = AppointmentsToText(appointsByDistrict);
                    responses.AddRange(responseDist);
                }
            }
            return responses;
        }

        private static IEnumerable<string> AppointmentsToText(Response<IEnumerable<Appointment>> appointments)
        {
            List<string> responses = new List<string>();
            var availableCenters = appointments.Content.SelectMany(q => q.centers).Where(q => q.sessions.Any(q => q.available_capacity_dose1 > 0 && q.available_capacity > 0 && q.min_age_limit == 18)).ToList();
            foreach (var center in availableCenters)
            {
                center.sessions = center.sessions.Where(q => q.available_capacity_dose1 > 0 && q.available_capacity > 0 && q.available_capacity == 18).ToArray();
            }
            var responseText = (new Appointment() { centers = availableCenters.ToArray() }).GenerateResponseMessage();

            if (!string.IsNullOrEmpty(responseText))
                responses.Add(responseText);

            return responses;
        }
    }
}
