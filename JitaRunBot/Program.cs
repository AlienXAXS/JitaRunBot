using System.Diagnostics;
using System.Net;
using JNogueira.Discord.Webhook.Client;
using SimpleTCP;

namespace JitaRunBot
{
    public class Program
    {
        public static bool AppIsRunning = true;
        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);
        private Game.LogWatcher _logWatcher;
        private Game.JitaRun _jitaRun;

        private static string Version = "0.6.0";

        public static void Main()
        {
            Util.Log("AGN JitaRun Bot Starting Up", Util.LogLevel.Info, ConsoleColor.White);
            new Program().MainMethod();
        }

        public void MainMethod()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => HandleApplicationExitEvent();
            Console.CancelKeyPress += (sender, args) => HandleApplicationExitEvent();
            
            Util.Log("[Twitch Auth] Loading Twitch Auth...", Util.LogLevel.Info);

            if (string.IsNullOrEmpty(Configuration.Handler.Instance.Config.TwitchClientSecret))
            {
                Util.Log("I do not have a twitch client secret, get this from the application developer!", Util.LogLevel.Error);
                Configuration.Handler.Instance.Config.TwitchAuthToken = null;
                Configuration.Handler.Instance.Save();
                Console.ReadKey();
                return;
            }

            if ( string.IsNullOrEmpty(Configuration.Handler.Instance.Config.TwitchAuthToken) || string.IsNullOrEmpty(Configuration.Handler.Instance.Config.TwitchUsername) )
            {
                /*
                var handler = new Twitch.TwitchCallbackHandler();
                try
                {
                    Util.Log("[Twitch Auth] Please provide your Twitch username, this will be used to auth the Twitch Bot", Util.LogLevel.Info);
                    var usernameTemp = "JitaRunBot"; //Console.ReadLine();

                    if (string.IsNullOrEmpty(usernameTemp))
                    {
                        Util.Log("[Twitch Auth] Invalid username provided - Press any key to exit", Util.LogLevel.Fatal);
                        Console.ReadKey();
                        return;
                    }

                    Configuration.Handler.Instance.Config.TwitchUsername = usernameTemp;

                    Util.Log($"[Twitch Auth] Twitch username is: {usernameTemp}, you will now be asked to authorise with Twitch in your web browser.  Press enter to continue", Util.LogLevel.Info);
                    Console.ReadLine();
                    handler.GetTwitchToken();

                    Util.Log("[Twitch Auth] Auth Token Successfully Read", Util.LogLevel.Info);
                }
                catch (Exception Exception)
                {
                    Console.WriteLine();
                }
                */

                Util.Log("Unable to connect to twitch, you need to generate a twitch token... Speak to the application developer!", Util.LogLevel.Error);
                return;
            }
            else
            {
                Twitch.TwitchHandler.Instance.Connect();
                Util.Log($"[Twitch Auth] Registered as {Configuration.Handler.Instance.Config.TwitchUsername}", Util.LogLevel.Info);
            }

            if (Configuration.Handler.Instance.Config.PilotName == null)
            {
                Util.Log($"Error, file {Configuration.Handler.Instance.GetConfigFilePath()} is not correctly configured, please configure this file and start the application again.\r\n\r\nPress enter to close this application.", Util.LogLevel.Fatal, ConsoleColor.Red);
                Console.ReadLine();
                _quitAppEvent.Set();
            }

            Util.Log($"Attempting to find latest game log file for today for Pilot {Configuration.Handler.Instance.Config.PilotName}", Util.LogLevel.Info);
            _logWatcher = new Game.LogWatcher();
            bool currentLogFileFound;

            try
            {
                currentLogFileFound = _logWatcher.LocateCurrentLogFile();
                _jitaRun = new Game.JitaRun(_logWatcher);
            } catch (Exception ex)
            {
                Util.Log(ex.ToString(), Util.LogLevel.Fatal, ConsoleColor.Red);
                return;
            }

            if (!currentLogFileFound)
            {
                Util.Log("Cannot find the latest log file!", Util.LogLevel.Error, ConsoleColor.Red);
                _quitAppEvent.WaitOne();
                return;
            }
            else
            {
                Util.Log($"Found game log file {_logWatcher.GetCurrentFileName()}", Util.LogLevel.Info, ConsoleColor.Green);
                Util.Log("Ready!", Util.LogLevel.Info, ConsoleColor.Green);
            }

            if (DiscordWebHookHandler.Instance.IsConfigured())
            {
                new Thread((ThreadStart) async delegate
                {
                    var whMessage = new DiscordMessage(
                        "",
                        avatarUrl: "https://agngaming.com/private/jitarun/JitarunBot256x256.png",
                        embeds: new[]
                        {
                            new DiscordMessageEmbed(
                                "Jita Run Bot",
                                description: "Bot started, configuration and status parameters below",
                                color: 0xFAEFBA,
                                thumbnail: new DiscordMessageEmbedThumbnail(
                                    "https://agngaming.com/private/jitarun/Info256x256.png"),
                                fields: new[]
                                {
                                    new DiscordMessageEmbedField("Bot Version",
                                        $"[v{Version}](https://github.com/AlienXAXS/JitaRunBot/releases)"),
                                    new DiscordMessageEmbedField("Pilot Name",
                                        Configuration.Handler.Instance.Config.PilotName),
                                    new DiscordMessageEmbedField("Status", "Waiting for undock")
                                }
                            )
                        });

                    await DiscordWebHookHandler.Instance.GetDiscordWebHook().SendToDiscord(whMessage);
                }).Start();
            }

            Util.Log("\r\n\r\nNotice:\r\nThis application will only detect a new Jita Run from a DOCKED state.\r\nPlease ensure you undock AFTER starting the application.\r\nWhen your ship changes to IN_JITA state you're good to go!", Util.LogLevel.Info, ConsoleColor.Black, ConsoleColor.Yellow);


            _quitAppEvent.WaitOne();
            AppIsRunning = false;
            _logWatcher.Dispose();
        }

        private void HandleApplicationExitEvent()
        {
            Util.Log(" -> Application Exit Event Invoked... Shutting down!",Util.LogLevel.Info);
            Util.Log("Goodbye!",Util.LogLevel.Info);
            Task.Delay(1000).GetAwaiter().GetResult();
            _quitAppEvent.Set();
        }
    }
}

