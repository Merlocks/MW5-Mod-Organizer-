using MW5_Mod_Organizer_WPF.Models;
using System;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class LoggerService
    {
        public static void AddLog(string name, string description)
        {
            Log log = new Log
            {
                Name = name,
                Description = description,
                DateTime = DateTime.Now.ToString("HH:mm:ss"),
            };

            Console.WriteLine($"*** Log message *** {log.Name}: {log.Description}");
        }
    }
}
