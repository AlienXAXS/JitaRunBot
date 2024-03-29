﻿using Newtonsoft.Json;
using JitaRunBot.Game;

namespace JitaRunBot.Configuration
{
    public class Handler
    {
        public static Handler Instance = _instance ??= new Handler();
        private static readonly Handler? _instance;

        private int ConfigVersion = 2;

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
                Util.Log($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", Util.LogLevel.Fatal, ConsoleColor.Red);
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

                        // Check if the config version has changed, if so we save the new file.
                        if ( Config.ConfigVersion != ConfigVersion )
                        {
                            Util.Log("Warning: Configuration version change detected, please ensure new elements are filled in!", Util.LogLevel.Warn);
                            Save();
                        }

                    } else
                    {
                        // Create the file if it does not exist, for formatting help.
                        Config.JitaOutGateSystems = new List<string>()
                        {
                            "Ikuchi",
                            "Sobaseki",
                            "Muvolailen",
                            "Maurasi",
                            "New Caldari",
                            "Niyabainen",
                            "Perimeter"
                        };
                        Save();
                    }
                }
            } catch (Exception ex)
            {
                Util.Log($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", Util.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

        public void Save()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Config.ConfigVersion = ConfigVersion;
                    File.WriteAllText(FullPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
                }
            } catch (Exception ex)
            {
                Util.Log($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", Util.LogLevel.Fatal, ConsoleColor.Red);
            }
        }
    }

    [Serializable]
    public class Config
    {
        public int? ConfigVersion { get; set; }
        public string? PilotName { get; set;}
        public string? DiscordWebHookUrl { get; set; }

        private string? _twitchUsername;

        public string? TwitchClientSecret { get; set; }

        [Serializable]
        public class LastKnownStateCls
        {
            public SystemType? PreviousSystem;
            public SystemType? CurrentSystem;
            public SystemType? StartingSystem;
            public int? totalJumps;
            public string? ShipState;
        }

        public LastKnownStateCls? LastKnownState;

        public string? TwitchUsername
        {
            get => _twitchUsername;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _twitchUsername = value;
                    Handler.Instance?.Save();
                }
            }
        }

        private string? _twitchAuthToken;
        public string? TwitchAuthToken {
            get => _twitchAuthToken;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _twitchAuthToken = value;
                    Handler.Instance?.Save();
                }
            }
        }

        private string? _twitchRefreshToken;
        public string? TwitchRefreshToken
        {
            get => _twitchRefreshToken;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _twitchRefreshToken = value;
                    Handler.Instance?.Save();
                }
            }
        }

        private DateTime? _twitchTokenExpiry;
        public DateTime? TwitchTokenExpiry
        {
            get => _twitchTokenExpiry;
            set
            {
                if (value != null)
                {
                    _twitchTokenExpiry = value;
                    Handler.Instance?.Save();
                }
            }
        }

        public List<string>? JitaOutGateSystems;
    }
}
