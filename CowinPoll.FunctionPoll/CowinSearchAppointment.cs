using CowinPoll.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;

namespace CowinPoll
{
    public static class CowinSearchAppointment
    {
        static AppSettings appsettings = new AppSettings
        {
            LatestOffsetId = 0,
            MaxDistrict = 2,
            MaxPincode = 2,
            SearchDistrict = Enumerable.Empty<string>().ToArray(),
            SearchPincode = Enumerable.Empty<string>().ToArray(),
            StartChatId = 0
        };

        [FunctionName("CowinPollAppointment")]
        public static void RunAsync([TimerTrigger("0 0 */2 * * *")] TimerInfo myTimer, ILogger log)
        {
            var bot = new TelegramBotClient(GetEnvironmentVariable("BotToken"));
            var updateTask = bot.GetUpdatesAsync(offset: appsettings.LatestOffsetId);
            updateTask.Wait();
            var updates = updateTask.Result;

            var latestStartMessage = updates.Where(q => q.Message != null && q.Message.Text != null && q.Message.Text.StartsWith("/start"))
                                            .OrderBy(r => r.Id)
                                            .FirstOrDefault();

            var latestStopMessage = updates.Where(q => q.Message != null && q.Message.Text != null && q.Message.Text.StartsWith("/stop"))
                                            .OrderBy(r => r.Id)
                                            .FirstOrDefault();

            if (latestStartMessage != null)
            {
                appsettings.StartChatId = latestStartMessage.Message.Chat.Id;
                appsettings.LatestOffsetId = latestStartMessage.Id + 1;
                var searchParam = latestStartMessage.Message.Text.Replace("/start", "").Replace("@GopCovSearch_bot","");
                var searchArray = searchParam.Split(",", StringSplitOptions.RemoveEmptyEntries);
                log.LogInformation($"{DateTime.Now}: Recieved request to search for {string.Join(",", searchArray)}");

                if (searchArray.Length == 0) bot.SendTextMessageAsync(appsettings.StartChatId, $"Sendformat /start<DISTRICTCODE>,<PINCODE> send upto {appsettings.MaxDistrict} values");
                else
                {
                    var pincode = new List<string>();
                    var dists = new List<string>();

                    searchArray.ToList().ForEach(q =>
                    {
                        var code = q.Trim();
                        if (code.Length == 6)
                        {
                            if (pincode.Count < appsettings.MaxPincode)
                                pincode.Add(code);
                        }
                        else
                        {
                            if (dists.Count < appsettings.MaxDistrict)
                                dists.Add(code);
                        }
                    });
                    appsettings.SearchPincode = pincode.ToArray();
                    appsettings.SearchDistrict = dists.ToArray();
                    bot.SendTextMessageAsync(appsettings.StartChatId, $"Bot will search for {string.Join(",", appsettings.SearchDistrict)}, {string.Join(",", appsettings.SearchPincode)}");
                }
            }

            if (latestStopMessage != null && (latestStopMessage.Id >= appsettings.LatestOffsetId))
            {
                appsettings.StartChatId = latestStopMessage.Message.Chat.Id;
                appsettings.LatestOffsetId = latestStopMessage.Id + 1;
                bot.SendTextMessageAsync(appsettings.StartChatId, $"Bot will stop search");
                appsettings.SearchDistrict = Enumerable.Empty<string>().ToArray(); appsettings.SearchPincode = Enumerable.Empty<string>().ToArray();
                log.LogInformation($"{DateTime.Now}: Recieved request to stop search");
            }

            if ((appsettings.SearchPincode.Length > 0 || appsettings.SearchDistrict.Length > 0) && Math.Abs(appsettings.StartChatId) > 0)
            {

                bot.SendChatActionAsync(appsettings.StartChatId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                var appointmentResponse = CowinTrigger.GetCowinResponses(appsettings.SearchPincode, appsettings.SearchDistrict);

                if (appointmentResponse.Keys.Count > 0)
                {
                    log.LogInformation($"{DateTime.Now}: Responding to search for {string.Join(",", appointmentResponse.Keys)}");
                    foreach (var key in appointmentResponse.Keys)
                    {
                        var responseText = appointmentResponse[key];
                        if (responseText.Length > 4090)
                        {
                            var chunk = 0;
                            for (int i = 0; i < responseText.Length; i += 4090)
                            {
                                bot.SendTextMessageAsync(appsettings.StartChatId, $"[{key}]  {responseText.Substring(i, 4090)}");
                                chunk++;
                                if (chunk > 5)
                                    break;
                            }
                        }
                        bot.SendTextMessageAsync(appsettings.StartChatId, $"[{key}]  {responseText}", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                }
            }
        }

        private static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }


    }


}
