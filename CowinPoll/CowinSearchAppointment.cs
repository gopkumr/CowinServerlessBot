using CowinPoll.Models;
using CowinPoll.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using Telegram.Bot;

namespace CowinPoll
{
    public static class CowinSearchAppointment
    {
        static int updateId = 0;

        [FunctionName("CowinSearchAppointment")]
        public static void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Function executed at: {DateTime.Now}");

            var bot = new TelegramBotClient(GetEnvironmentVariable("BotToken"));
            var getUpdatedResponse = bot.GetUpdatesAsync(updateId);
            getUpdatedResponse.Wait();
            var updates = getUpdatedResponse.Result;
            updates.ToList().ForEach(q =>
            {
                updateId = q.Id + 1;
                if (q.Message != null)
                {
                    bot.SendChatActionAsync(q.Message.Chat.Id, Telegram.Bot.Types.Enums.ChatAction.Typing);
                    var chatMessage = q.Message.Text;
                    log.LogInformation($"Got message: " + chatMessage);
                    if (chatMessage.StartsWith("/search"))
                    {
                        chatMessage = chatMessage.Replace("/search", "").Trim();
                        var splits = chatMessage.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        if (splits.Length == 0) bot.SendTextMessageAsync(q.Message.Chat.Id, $"Sendformat /search<PINCODE>,<dd-mm-yyyy>");
                        else
                        {
                            var pincode = splits.First();
                            var date = DateTime.Now.ToString("dd-MM-yyyy");
                            if (splits.Length > 1)
                                date = splits.Last();

                            var appointmentResponse = new CowinService().GetAppointment(chatMessage, date);
                            if (appointmentResponse.Success)
                            {
                                var responseText = GenerateResponseMessage(appointmentResponse.Content, chatMessage);
                                bot.SendTextMessageAsync(q.Message.Chat.Id, responseText, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            }
                            else
                            {
                                bot.SendTextMessageAsync(q.Message.Chat.Id, $"Failed getting data from COWIN: {appointmentResponse.ErrorMessage}");
                            }
                        }
                    }
                    else
                    {
                        bot.SendTextMessageAsync(q.Message.Chat.Id, $"Sendformat /search<PINCODE>,<dd-mm-yyyy>");
                    }
                }
            });
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
                sb.AppendLine("-----------------------------------");
            }

            return sb.ToString();
        }
    }


}
