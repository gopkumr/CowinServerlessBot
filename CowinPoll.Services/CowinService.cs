using CowinPoll.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace CowinPoll.Services
{
    public class CowinService
    {
        static HttpClient client = new HttpClient();

        public Response<Appointment> GetAppointmentByPin(string pinCode, string dateValue)
        {
            var result = new Response<Appointment>() { Success = true };

            var response = client.GetAsync($"https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByPin?pincode={pinCode}&date={dateValue}");
            response.Wait();
            if (response.Result.IsSuccessStatusCode)
            {
                var readResponse = response.Result.Content.ReadAsAsync<Appointment>();
                readResponse.Wait();
                result.Content = readResponse.Result;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = response.Result.ReasonPhrase;
            }

            return result;
        }

        public Response<Appointment> GetAppointmentByDistrict(string districtCode, string dateValue)
        {
            var result = new Response<Appointment>() { Success = true };

            var response = client.GetAsync($"https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByDistrict?district_id={districtCode}&date={dateValue}");
            response.Wait();
            if (response.Result.IsSuccessStatusCode)
            {
                var readResponse = response.Result.Content.ReadAsAsync<Appointment>();
                readResponse.Wait();
                result.Content = readResponse.Result;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = response.Result.ReasonPhrase;
            }

            return result;
        }

        public Response<IEnumerable<Appointment>> GetAppointmentByPinFor21Days(IEnumerable<string> pinCodes)
        {
            var appointmentUrl = "https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByPin?pincode={0}&date={1}";
            var result = CallAppointmentUrlFor21Days(pinCodes, appointmentUrl);
            return result;
        }

        public Response<IEnumerable<Appointment>> GetAppointmentByDistrictFor21Days(IEnumerable<string> districtCodes)
        {
            var appointmentUrl = "https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByDistrict?district_id={0}&date={1}";
            var result = CallAppointmentUrlFor21Days(districtCodes, appointmentUrl);
            return result;
        }

        private static Response<IEnumerable<Appointment>> CallAppointmentUrlFor21Days(IEnumerable<string> codes, string appointmentUrl)
        {
            var result = new Response<IEnumerable<Appointment>>() { Success = true };
            var dates = new List<DateTime>
            {
                DateTime.Today,
                DateTime.Today.AddDays(8),
                DateTime.Today.AddDays(16)
            };
            var tasksArray = new List<Task<HttpResponseMessage>>();

            dates.ForEach(date =>
            {
                codes.ToList().ForEach(code =>
                {
                    tasksArray.Add(client.GetAsync(string.Format(appointmentUrl, code, date.ToString("dd-MM-yyyy"))));
                });
            });

            Task.WaitAll(tasksArray.ToArray());

            if (tasksArray.Any(q => q.Result.IsSuccessStatusCode))
            {
                var responseTasks = tasksArray.Select(q => q.Result.Content.ReadAsAsync<Appointment>());
                Task.WaitAll(responseTasks.ToArray());

                result.Content = responseTasks.Select(q => q.Result);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Error fetching data";
            }

            return result;
        }
    }
}
