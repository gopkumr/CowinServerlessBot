using CowinPoll.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Timers;
using Telegram.Bot;
using System.Linq;
using CowinPoll.Services;
using System.Collections.Generic;

namespace CowinPoll.Server
{
    class Program
    {
        static AppSettings appsettings;
        static DateTime LastCowinRun = DateTime.MinValue;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            appsettings = SettingsFileManager.ReadSettingsFile();

            var timer = new Timer(1 * 60000);
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
            var bot = new TelegramBotClient("");
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
                var searchParam = latestStartMessage.Message.Text.Replace("/start", "");
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

            if ((appsettings.SearchPincode.Length > 0 || appsettings.SearchDistrict.Length > 0) && appsettings.StartChatId > 0)
            {
                if (LastCowinRun == DateTime.MinValue || DateTime.Now.Subtract(LastCowinRun).Minutes >= appsettings.IntervalMinutes)
                {
                    bot.SendChatActionAsync(appsettings.StartChatId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                    var appointmentResponse = CowinTrigger.GetCowinResponses(appsettings.SearchPincode, appsettings.SearchDistrict);
                   
                    if (appointmentResponse.Keys.Count > 0)
                    {
                        Console.WriteLine($"{DateTime.Now}: Responding to search for {string.Join(",", appointmentResponse.Keys)}");
                        foreach (var key in appointmentResponse.Keys)
                        {
                            var responseText = appointmentResponse[key];
                            if (responseText.Length > 4090)
                            {
                                var chunk = 0;
                                for (int i = 0; i < responseText.Length; i += 4090)
                                {
                                    bot.SendTextMessageAsync(appsettings.StartChatId, $"{key} {responseText.Substring(i, 4090)}");
                                    chunk++;
                                    if (chunk > 5)
                                        break;
                                }
                            }
                            bot.SendTextMessageAsync(appsettings.StartChatId,$"{key} {responseText}", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                        LastCowinRun = DateTime.Now;
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}: Failed to get data from COWIN.");
                        bot.SendTextMessageAsync(appsettings.StartChatId, $"Sorry! failed getting data from COWIN");
                    }
                }
            }
        }
    }
}