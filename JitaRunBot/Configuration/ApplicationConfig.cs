using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JitaRunBot.Configuration
{
    public class Handler
    {
        public static Handler Instance = _instance ?? (_instance = new Handler());
        private static readonly Handler _instance;

        public Config Config = new Config();

        private string RootPath;
        private string FullPath;
        private string FileName = "config.json";

        public Handler()
        {
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JitaRunBot");
            FullPath = Path.Combine(RootPath, FileName);

            try
            {
                if (!Directory.Exists(RootPath))
                {
                    Directory.CreateDirectory(RootPath);
                }
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }

            Load();
        }

        public string GetConfigFilePath()
        {
            return FullPath;
        }

        private void Load()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    if (File.Exists(FullPath))
                    {
                        Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(FullPath));
                    } else
                    {
                        // Create the file if it does not exist, for formatting help.
                        Save();
                    }
                }
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }
        }

        private void Save()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    File.WriteAllText(FullPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
                }
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }
        }
    }

    [Serializable]
    public class Config
    {
        public string? PilotName { get; set;}
        public string? DiscordWebHookUrl { get; set; }

        public List<string> JitaOutGateSystems = new List<string>()
        {
            "Ikuchi",
            "Sobaseki",
            "Muvolailen",
            "Maurasi",
            "New Caldari",
            "Niyabainen",
            "Perimeter"
        };
    }
}
