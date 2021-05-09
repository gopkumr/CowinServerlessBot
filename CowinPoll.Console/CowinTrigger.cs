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
                    var availableCenters = appointmentResponse.Content.Centers.ToList().Where(q => q.Sessions.Any(q => q.AvailableCapacity > 0)).ToList();
                    foreach (var center in availableCenters)
                    {
                        center.Sessions = center.Sessions.Where(q => q.AvailableCapacity > 0).ToArray();
                    }
                    var responseText = (new Appointment() { Centers = availableCenters.ToArray() }).GenerateResponseMessage(pin);
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
                    var availableCenters = appointmentResponse.Content.Centers.ToList().Where(q => q.Sessions.Any(q => q.AvailableCapacity > 0)).ToList();
                    foreach (var center in availableCenters)
                    {
                        center.Sessions = center.Sessions.Where(q => q.AvailableCapacity > 0).ToArray();
                    }
                    var responseText = (new Appointment() { Centers = availableCenters.ToArray() }).GenerateResponseMessage(dist);
                    responses.Add(dist, responseText);
                }
                else
                    responses.Add(dist, appointmentResponse.ErrorMessage);
            }

            return responses;
        }
    }
}
