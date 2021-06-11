using CowinPoll.Models;
using CowinPoll.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CowinPoll
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
                else
                    responses.Add(pin, appointmentResponse.ErrorMessage);
            }

            foreach (var dist in districts)
            {
                if (responses.ContainsKey(dist)) continue;
                appointmentResponse = new CowinService().GetAppointmentByDistrict(dist, date);
                if (appointmentResponse.Success)
                {
                    var availableCenters = appointmentResponse.Content.centers.ToList().Where(q => q.sessions.Any(q => q.available_capacity > 0 && q.min_age_limit==18)).ToList();
                    foreach (var center in availableCenters)
                    {
                        center.sessions = center.sessions.Where(q => q.available_capacity > 0 && q.min_age_limit == 18).ToArray();
                    }
                    var responseText = (new Appointment() { centers = availableCenters.ToArray() }).GenerateResponseMessage();
                    if (!string.IsNullOrEmpty(responseText))
                        responses.Add(dist, responseText);
                }
                else
                    responses.Add(dist, appointmentResponse.ErrorMessage);
            }

            return responses;
        }
    }
}
