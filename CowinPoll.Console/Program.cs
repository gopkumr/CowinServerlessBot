using CowinPoll.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Timers;
using Telegram.Bot;
using System.Linq;
using CowinPoll.Services;
using System.Collections.Generic;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;

namespace CowinPoll.Server
{
    class Program
    {
        static AppSettings appsettings;
        static DateTime LastCowinRun = DateTime.MinValue;
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            appsettings = SettingsFileManager.ReadSettingsFile();

            var timer = new Timer(appsettings.IntervalMinutes * 60000);
            timer.Elapsed += CowinPoll;
            timer.Enabled = true;
            timer.Start();
            Console.WriteLine($"Starting the server: {DateTime.Now}, Press X to quit.");

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.X)
                {
                    timer.Stop();
                    break;
                }
            }

        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            SettingsFileManager.WriteSettingsFile(appsettings);
        }

        private static void CowinPoll(object sender, ElapsedEventArgs e)
        {
            var bot = new TelegramBotClient("1880774312:AAFN7eWptmcqAN1X29kiJpUQqhI9HqcMFdg");
            var updateTask = bot.GetUpdatesAsync(offset: appsettings.LatestOffsetId, allowedUpdates: new List<UpdateType>() { UpdateType.Message });
            updateTask.Wait();
            var updates = updateTask.Result;

            var latestStartMessage = updates.Where(q => q.Message != null && q.Message.Text != null && q.Message.Text.StartsWith("/start"))
                                            .OrderByDescending(r => r.Id)
                                            .FirstOrDefault();

            var latestStopMessage = updates.Where(q => q.Message != null && q.Message.Text != null && q.Message.Text.StartsWith("/stop"))
                                            .OrderByDescending(r => r.Id)
                                            .FirstOrDefault();
            
             if (latestStartMessage != null)
            {
                appsettings.StartChatId = latestStartMessage.Message.Chat.Id;
                appsettings.LatestOffsetId = latestStartMessage.Id + 1;
                var searchParam = latestStartMessage.Message.Text.Replace("/start", "").Replace("@GopCovSearch_bot", "");
                var searchArray = searchParam.Split(",", StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"{DateTime.Now}: Recieved request to search for {string.Join(",", searchArray)}");

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

            if (latestStopMessage != null && (latestStopMessage.Id >= appsettings.LatestOffsetId)) {
                appsettings.StartChatId = latestStopMessage.Message.Chat.Id;
                appsettings.LatestOffsetId = latestStopMessage.Id + 1;
                bot.SendTextMessageAsync(appsettings.StartChatId, $"Bot will stop search");
                appsettings.SearchDistrict = Enumerable.Empty<string>().ToArray(); appsettings.SearchPincode = Enumerable.Empty<string>().ToArray();
                Console.WriteLine($"{DateTime.Now}: Recieved request to stop search");
            }

            if ((appsettings.SearchPincode.Length > 0 || appsettings.SearchDistrict.Length > 0) && Math.Abs(appsettings.StartChatId) > 0)
            {
                Console.WriteLine($"Bot will search for {string.Join(",", appsettings.SearchDistrict)}, {string.Join(",", appsettings.SearchPincode)}");
                {
                    bot.SendChatActionAsync(appsettings.StartChatId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                    var appointmentResponse = CowinTrigger.GetCowinResponsesFor21Days(appsettings.SearchPincode, appsettings.SearchDistrict);
                   
                    if (appointmentResponse.Count() > 0)
                    {
                        Console.WriteLine($"{DateTime.Now}: Responding to search for {string.Join(",", appsettings.SearchPincode)},{string.Join(",", appsettings.SearchDistrict)}");
                        foreach (var val in appointmentResponse)
                        {
                            var responseText = val;
                            if (responseText.Length > 4090)
                            {
                                var chunk = 0;
                                for (int i = 0; i < responseText.Length; i += 4090)
                                {
                                    bot.SendTextMessageAsync(appsettings.StartChatId, $"{responseText.Substring(i, 4090)}", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                    chunk++;
                                    if (chunk > 5)
                                        break;
                                }
                            }
                            bot.SendTextMessageAsync(appsettings.StartChatId,$"{responseText}", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                        LastCowinRun = DateTime.Now;
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}: Failed to get data from COWIN.");
                    }
                }
            }
        }
    }
}