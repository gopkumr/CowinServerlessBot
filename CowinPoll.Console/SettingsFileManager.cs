using CowinPoll.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CowinPoll.Server
{
    public static class SettingsFileManager
    {
        public static AppSettings ReadSettingsFile()
        {
            AppSettings settings = new AppSettings() { IntervalMinutes = 60 };
            var file = new FileInfo("appsettings.json");
            using (var streamReader = new StreamReader(file.OpenRead()))
            {
                var jsonString = streamReader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<AppSettings>(jsonString);
            }

            return settings;
        }

        public static void WriteSettingsFile(AppSettings settings)
        {
            var file = new FileInfo("appsettings.json");
            using (var streamWriter = new StreamWriter(file.FullName,append:false))
            {
                streamWriter.WriteLine(JsonConvert.SerializeObject(settings));
                streamWriter.Flush();
            }
        }
    }
}
