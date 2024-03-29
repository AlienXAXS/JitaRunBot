﻿using JNogueira.Discord.Webhook.Client;

namespace JitaRunBot.Game
{
    internal class JitaRun
    {
        private int _totalJumps;
        private SystemType _currentSystem;
        private SystemType _previousSystem;
        private SystemType _startingSystem;

        private bool _isRunActive = false;

        public Status Results;
        public enum Status
        {
            WIN,
            LOSS
        }

        private ShipStatusEnum _shipStatusValue = ShipStatusEnum.UNKNOWN;
        private ShipStatusEnum _shipStatus
        {
            get { return _shipStatusValue; }
            set {
                if ( _shipStatusValue != value ) 
                    Util.Log($"Ship Status Changed To {value}", Util.LogLevel.Info, ConsoleColor.Yellow);

                _shipStatusValue = value;
            }
        }

        public enum ShipStatusEnum
        { 
            IN_JITA,
            DOCKED_IN_JITA,
            IN_FLIGHT,
            UNKNOWN
        }

        public JitaRun(LogWatcher logWatcher)
        {
            logWatcher.OnFileChanged += LogWatcher_OnFileChanged;

            var lastKnownState = Configuration.Handler.Instance.Config.LastKnownState;
            if (lastKnownState != null)
            {
                new Thread((ThreadStart)async delegate
                {
                    var whMessage = new DiscordMessage(
                        "",
                        avatarUrl: "https://agngaming.com/private/jitarun/JitarunBot256x256.png",
                        embeds: new[]
                        {
                            new DiscordMessageEmbed(
                                "Jita Run Bot",
                                description: "Previous JitaRun State Found!",
                                color: 0xFAEFBA,
                                thumbnail: new DiscordMessageEmbedThumbnail("https://agngaming.com/private/jitarun/Info256x256.png"),
                                fields: new []
                                {
                                    new DiscordMessageEmbedField("Starting System", lastKnownState.StartingSystem.Name),
                                    new DiscordMessageEmbedField("Previous System", lastKnownState.PreviousSystem.Name),
                                    new DiscordMessageEmbedField("Current System", lastKnownState.CurrentSystem.Name),
                                    new DiscordMessageEmbedField("Total Jumps Done So Far", lastKnownState.totalJumps.ToString()),
                                    new DiscordMessageEmbedField("Last Ship State", lastKnownState.ShipState)
                                }
                            )
                        });

                    await DiscordWebHookHandler.Instance.GetDiscordWebHook().SendToDiscord(whMessage);
                }).Start();
            }
        }

        private void LogWatcher_OnFileChanged(object source, FileContentsChanged e)
        {
            AnalyzeLine(e.GetInfo());
        }

        private void AnalyzeLine(string line)
        {
            try
            {
                // Ensure we get a line we can split on.
                if (line.Any(x => x.Equals(']') && line.Any(x => x.Equals(')'))))
                {
                    var timestampSplit = line.Split(']');
                    var currentLine = timestampSplit[1].Trim();
                    var actionType = currentLine.Substring(1, currentLine.IndexOf(')') - 1);

                    var actionMessage = currentLine.Substring(currentLine.IndexOf(')') + 1).Trim();
                    switch (actionType.ToLower())
                    {
                        case "none":
                            HandleNoneActionType(actionMessage);
                            break;

                        case "notify":
                            HandleNotifyActionType(actionMessage);
                            break;

                        case "combat":
                            // We no longer handle Combat because CCP log utter shite to the log file...
                            // CCPlease... Why u so bad?
                            break;
                    }
                }
            } catch (Exception ex)
            {
                Util.Log($"Fatal error when reading & Analyzing line contents\r\n\r\nLine: {line}\r\nError: {ex.Message}\r\n\r\nStack:\r\n{ex.StackTrace}", Util.LogLevel.Error, ConsoleColor.Red);
            }
        }

        private void HandleNoneActionType(string line)
        {
            // Jumping from ZA0L-U to C-FP70
            if ( line.StartsWith("Jumping from") )
            {
                // Jumping from one system to another
                line = line.Substring("Jumping from".Length).Trim();
                var systemNames = line.Split(" to ");

                _previousSystem = new SystemType(systemNames[0]);
                _currentSystem = new SystemType(systemNames[1]);

                HandleSystemJump( _previousSystem, _currentSystem );
            }

            // Undocking from Jita IV - Moon 4 - Caldari Navy Assembly Plant to Jita solar system.
            if ( line.StartsWith("Undocking from") )
            {
                line = line.Substring("Undocking from".Length).Trim();
                var undockingSplit = line.Split(" to ");

                var undockStationName = undockingSplit[0];
                var undockSystemName = undockingSplit[1].Replace(" solar system.", "");

                Util.Log($"Undock Detected: You undocked from {undockingSplit[0]} in system {undockingSplit[1]}", Util.LogLevel.Info, ConsoleColor.Yellow);

                if (undockSystemName == "Jita")
                {
                    _shipStatus = ShipStatusEnum.IN_JITA;
                    _totalJumps = 0;
                }
            }

            CalculateResult();
        }

        private void HandleNotifyActionType(string line)
        {
            // Requested to dock at Jita IV - Moon 4 - Caldari Navy Assembly Plant station
            if ( line.StartsWith("Requested to dock at") )
            {
                // Docking Request
                line = line.Substring("Requested to dock at ".Length).Trim();

                Util.Log($"Docking Request Detected: {_currentSystem} -> {line}", Util.LogLevel.Info, ConsoleColor.Yellow);

                if (line == "Jita IV - Moon 4 - Caldari Navy Assembly Plant station")
                    _shipStatus = ShipStatusEnum.DOCKED_IN_JITA;
            }
        }
        
        private void HandleSystemJump(SystemType previousSystem, SystemType newSystem)
        {
            if (_isRunActive)
                _totalJumps++;

            var statusMsg = $"System Jump Detected: {_previousSystem.Name} To {_currentSystem.Name}\r\nJumps: {_totalJumps} | Status:{Enum.GetName(_shipStatus)})";
            Util.Log(statusMsg, Util.LogLevel.Info, ConsoleColor.Yellow);

            if (_isRunActive && Configuration.Handler.Instance.Config.LastKnownState != null)
            {
                // Save the current state to the config file.
                Configuration.Handler.Instance.Config.LastKnownState.CurrentSystem = _currentSystem;
                Configuration.Handler.Instance.Config.LastKnownState.PreviousSystem = _previousSystem;
                Configuration.Handler.Instance.Config.LastKnownState.StartingSystem = _startingSystem;
                Configuration.Handler.Instance.Config.LastKnownState.totalJumps = _totalJumps;
                Configuration.Handler.Instance.Config.LastKnownState.ShipState = Enum.GetName(_shipStatus);
                Configuration.Handler.Instance.Save();
            }

            // Detect if we were in Jita, and we are now in a null-sec system.
            if ( _shipStatus == ShipStatusEnum.IN_JITA )
            {
                if (previousSystem.SecurityLevel == SystemType.SystemSecurity.NULLSEC)
                {
                    // We are in a Jita Run!
                    _shipStatus = ShipStatusEnum.IN_FLIGHT;
                    _isRunActive = true;
                    _totalJumps = 1; //Set our total jumps to 1 here, as we were not active earlier, and we have already done one jump.

                    _startingSystem = previousSystem;

                    Util.Log(@"           _ _____ _______       _____  _    _ _   _ ", Util.LogLevel.Info, ConsoleColor.Green);
                    Util.Log(@"          | |_   _|__   __|/\   |  __ \| |  | | \ | |", Util.LogLevel.Info, ConsoleColor.Green);
                    Util.Log(@"          | | | |    | |  /  \  | |__) | |  | |  \| |", Util.LogLevel.Info, ConsoleColor.Green);
                    Util.Log(@"      _   | | | |    | | / /\ \ |  _  /| |  | | . ` |", Util.LogLevel.Info, ConsoleColor.Green);
                    Util.Log(@"     | |__| |_| |_   | |/ ____ \| | \ \| |__| | |\  |", Util.LogLevel.Info, ConsoleColor.Green);
                    Util.Log(@"     \____ /|_____|  |_/_/    \_\_|  \_\\____/|_| \_|", Util.LogLevel.Info, ConsoleColor.Green);
                    Util.Log(@"                                 STARTED, HERE WE GO!", Util.LogLevel.Info, ConsoleColor.Green);

                    Configuration.Handler.Instance.Config.LastKnownState = new Configuration.Config.LastKnownStateCls();

                    new Thread((ThreadStart)async delegate
                    {
                        var whMessage = new DiscordMessage(
                            "",
                            avatarUrl: "https://agngaming.com/private/jitarun/JitarunBot256x256.png",
                            embeds: new[]
                            {
                                new DiscordMessageEmbed(
                                    "Jita Run Bot",
                                    description: "New JitaRun Started!",
                                    color: 0xFAEFBA,
                                    thumbnail: new DiscordMessageEmbedThumbnail("https://agngaming.com/private/jitarun/Info256x256.png"),
                                    fields: new []
                                    {
                                        new DiscordMessageEmbedField("Pilot Name", Configuration.Handler.Instance.Config.PilotName),
                                        new DiscordMessageEmbedField("Status", "JitaRun Started"),
                                        new DiscordMessageEmbedField("Starting System", $"[{_startingSystem.Name}](https://evemaps.dotlan.net/system/{_startingSystem.Name})")
                                    }
                                )
                            });

                        await DiscordWebHookHandler.Instance.GetDiscordWebHook().SendToDiscord(whMessage);
                    }).Start();
                }
            }
        }

        /// <summary>
        /// Calculates the result, returns true if a result is found, or false if no action is needed.
        /// </summary>
        private void CalculateResult()
        {
            if (!_isRunActive) return;

            if (_currentSystem == null)
                return;

            // Did we win?
            if ( _currentSystem.Name == "Jita" )
            {
                if (WasPreviousSystemJitaConnected())
                {
                    // Add another jump here, as it's our first.
                    HandleJitaWin();
                }
                else
                {
                    if (_shipStatus == ShipStatusEnum.IN_FLIGHT || _shipStatus == ShipStatusEnum.IN_JITA)
                    {
                        HandleJitaLoss();
                    }
                }
            }

            // Did we arrive in Jita (undocked) from a system not next to Jita?
            if ( _shipStatus == ShipStatusEnum.IN_JITA )
            {
                if ( !WasPreviousSystemJitaConnected() )
                {
                    HandleJitaLoss();
                }
            }
        }

        private bool WasPreviousSystemJitaConnected()
        {
            return Configuration.Handler.Instance.Config.JitaOutGateSystems.Any(x => x.Equals(_previousSystem.Name));
        }

        private void HandleJitaWin()
        {
            Util.Log($"Jita Win Detected.  Total jumps: {_totalJumps}", Util.LogLevel.Info, ConsoleColor.Green);

            var tempJumpCount = _totalJumps;
            new Thread((ThreadStart)async delegate
            {
                var whMessage = new DiscordMessage(
                    "",
                    avatarUrl: "https://agngaming.com/private/jitarun/JitarunBot256x256.png",
                    embeds: new[]
                    {
                        new DiscordMessageEmbed(
                            "Jita Run Bot",
                            description: "JitaRun Success.  Details below!",
                            color: 0xA6FFD3,
                            thumbnail: new DiscordMessageEmbedThumbnail("https://agngaming.com/private/jitarun/Success256x256.png"),
                            fields: new []
                            {
                                new DiscordMessageEmbedField("Pilot Name", Configuration.Handler.Instance.Config.PilotName),
                                new DiscordMessageEmbedField("Status", "JitaRun Successful"),
                                new DiscordMessageEmbedField("Starting System", $"[{_startingSystem.Name}](https://evemaps.dotlan.net/system/{_startingSystem.Name})"),
                                new DiscordMessageEmbedField("Total Jumps", tempJumpCount.ToString()),
                                new DiscordMessageEmbedField("Twitch Command", $"`!jitawin")
                            }
                        )
                    });

                await DiscordWebHookHandler.Instance.GetDiscordWebHook().SendToDiscord(whMessage);
            })
            {
                IsBackground = true,
            }.Start();

            Twitch.TwitchHandler.Instance.SendChatMessage("!jitawin");

            ResetJitaRun();
        }

        private void HandleJitaLoss()
        {
            Util.Log($"Jita Loss Detected.  Total jumps: {_totalJumps}", Util.LogLevel.Info, ConsoleColor.Green);

            var tempJumpCount = _totalJumps;
            new Thread((ThreadStart)async delegate
            {
                var whMessage = new DiscordMessage(
                    "",
                    avatarUrl: "https://agngaming.com/private/jitarun/JitarunBot256x256.png",
                    embeds: new[]
                    {
                        new DiscordMessageEmbed(
                            "Jita Run Bot",
                            description: "JitaRun Failure.  Details below!",
                            color: 0xFFA6AD,
                            thumbnail: new DiscordMessageEmbedThumbnail("https://agngaming.com/private/jitarun/Fail256x256.png"),
                            fields: new []
                            {
                                new DiscordMessageEmbedField("Pilot Name", Configuration.Handler.Instance.Config.PilotName),
                                new DiscordMessageEmbedField("Status", "JitaRun Failure"),
                                new DiscordMessageEmbedField("Starting System", $"[{_startingSystem.Name}](https://evemaps.dotlan.net/system/{_startingSystem.Name})"),
                                new DiscordMessageEmbedField("Total Jumps", tempJumpCount.ToString()),
                                new DiscordMessageEmbedField("Twitch Command", $"`!jitafail`")
                            }
                        )
                    });

                await DiscordWebHookHandler.Instance.GetDiscordWebHook().SendToDiscord(whMessage);
            })
            {
                IsBackground = true,
            }.Start();

            Twitch.TwitchHandler.Instance.SendChatMessage("!jitafail");

            ResetJitaRun();
        }

        private void ResetLastKnownState()
        {
            Configuration.Handler.Instance.Config.LastKnownState = null;
            Configuration.Handler.Instance.Save();
        }

        private void ResetJitaRun()
        {
            _totalJumps = 0;
            _isRunActive = false;
            ResetLastKnownState();
            Util.Log("Full Jitarun Complete, waiting for next run...", Util.LogLevel.Info);
        }

    }

    public class SystemType
    {
        public SystemType(string name)
        {
            _name = name;
        }

        public enum SystemSecurity
        {
            NULLSEC,
            OTHER
        }

        private string? _name;
        public string Name
        { 
            get
            {
                return _name;
            }
            set
            {
                if (value.Length == 6 && value.Contains('-'))
                    SecurityLevel = SystemSecurity.NULLSEC;
                else
                    SecurityLevel = SystemSecurity.OTHER;

                _name = value;
            }
        }

        public SystemSecurity SecurityLevel;
    }
}
