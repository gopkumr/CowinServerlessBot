using CowinPoll.Models;
using CowinPoll.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CowinPoll
{
    public static class CowinSearchAppointment
    {
        static int updateId = 0;
        [FunctionName("CowinSearchAppointment")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CowinSearch/{token}")] HttpRequest request, string token, ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");
            bool isPin = false;

            var bot = new TelegramBotClient(GetEnvironmentVariable("BotToken"));

            var body = await request.ReadAsStringAsync();
            var update = JsonConvert.DeserializeObject<Update>(body);

            updateId = update.Id + 1;
            if (update.Message != null)
            {
                await bot.SendChatActionAsync(update.Message.Chat.Id, Telegram.Bot.Types.Enums.ChatAction.Typing);

                var chatMessage = update.Message.Text;
                log.LogInformation($"Got message: " + chatMessage);
                if (chatMessage.StartsWith("/dist") || chatMessage.StartsWith("/pin"))
                {
                    if (chatMessage.StartsWith("/pin"))
                    {
                        isPin = true;
                        chatMessage = chatMessage.Replace("/pin", "").Trim();
                    }
                    else
                        chatMessage = chatMessage.Replace("/dist", "").Trim();

                    var splits = chatMessage.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (splits.Length == 0) await bot.SendTextMessageAsync(update.Message.Chat.Id, $"Sendformat /dist<DISTRICTCODE> or /pin<PINCODE>");
                    else
                    {
                        Response<Appointment> appointmentResponse;
                        var code = splits.First();
                        var date = DateTime.Now.ToString("dd-MM-yyyy");
                        if (isPin)
                            appointmentResponse = new CowinService().GetAppointmentByPin(code, date);
                        else
                            appointmentResponse = new CowinService().GetAppointmentByDistrict(code, date);

                        if (appointmentResponse.Success)
                        {
                            var responseText = GenerateResponseMessage(appointmentResponse.Content, chatMessage);
                            if (responseText.Length > 4096)
                            {
                                var chunk = 0;
                                for (int i = 0; i < responseText.Length; i += 4096)
                                {
                                    await bot.SendTextMessageAsync(update.Message.Chat.Id, responseText.Substring(i, 4096));
                                    chunk++;
                                    if (chunk > 5)
                                        return new OkResult();
                                }
                                return new OkResult();
                            }
                            await bot.SendTextMessageAsync(update.Message.Chat.Id, responseText, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            return new OkResult();
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(update.Message.Chat.Id, $"Sorry! failed getting data from COWIN: {appointmentResponse.ErrorMessage}");
                            return new OkResult();
                        }
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(update.Message.Chat.Id, $"Sendformat /dist<DISTRICTCODE> or /pin<PINCODE>");
                    return new OkResult();
                }
            }
            return new OkResult();
        }

        private static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static string GenerateResponseMessage(Appointment appointmentData, string pincode)
        {
            var sb = new StringBuilder();
            if (appointmentData == null || appointmentData.Centers == null || appointmentData.Centers.Length == 0)
                sb.AppendLine($"*No appointment data available for pincode {pincode}*");
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
