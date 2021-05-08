using CowinPoll.Models;
using System.Net.Http;

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
    }
}
