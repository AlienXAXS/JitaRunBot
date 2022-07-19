using System.Diagnostics;
using System.Net;
using JNogueira.Discord.Webhook.Client;
using SimpleTCP;
using JitaRunBot.Twitch;

namespace JitaRunBot
{
    public class Program
    {
        private bool appIsRunning = true;
        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);
        private Game.LogWatcher _logWatcher;
        private Game.JitaRun _jitaRun;

        private static string Version = "0.5.1";

        public static void Main()
        {
            ConsoleUtil.WriteToConsole("AGN JitaRun Bot Starting Up", ConsoleUtil.LogLevel.INFO, ConsoleColor.White);
            new Program().MainMethod();
        }

        public void MainMethod()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => HandleApplicationExitEvent();
            Console.CancelKeyPress += (sender, args) => HandleApplicationExitEvent();

            SimpleTcpServer tcpServer = new SimpleTcpServer();
            tcpServer.Start(3273);
            ConsoleUtil.WriteToConsole("LogLiteProxy started on port 3273", ConsoleUtil.LogLevel.INFO);

            tcpServer.ClientConnected += (sender, client) =>
            {
                ConsoleUtil.WriteToConsole($"[LogLiteProxy] Client connected {((IPEndPoint)client.Client.RemoteEndPoint).Address}", ConsoleUtil.LogLevel.INFO);
            };

            tcpServer.DataReceived += (sender, message) =>
            {
                Debug.Print(message.MessageString);
            };

            ConsoleUtil.WriteToConsole("[Twitch Auth] Loading Twitch Auth...", ConsoleUtil.LogLevel.INFO);
            if ( string.IsNullOrEmpty(Configuration.Handler.Instance.Config.TwitchAuthToken) || string.IsNullOrEmpty(Configuration.Handler.Instance.Config.TwitchUsername) )
            {
                var handler = new Twitch.TwitchCallbackHandler();
                try
                {
                    ConsoleUtil.WriteToConsole("[Twitch Auth] Please provide your Twitch username, this will be used to auth the Twitch Bot", ConsoleUtil.LogLevel.INFO);
                    var usernameTemp = "JitaRunBot"; //Console.ReadLine();

                    if (string.IsNullOrEmpty(usernameTemp))
                    {
                        ConsoleUtil.WriteToConsole("[Twitch Auth] Invalid username provided - Press any key to exit", ConsoleUtil.LogLevel.FATAL);
                        Console.ReadKey();
                        return;
                    }

                    Configuration.Handler.Instance.Config.TwitchUsername = usernameTemp;

                    ConsoleUtil.WriteToConsole($"[Twitch Auth] Twitch username is: {usernameTemp}, you will now be asked to authorise with Twitch in your web browser.  Press enter to continue", ConsoleUtil.LogLevel.INFO);
                    Console.ReadLine();
                    handler.GetTwitchToken();

                    ConsoleUtil.WriteToConsole("[Twitch Auth] Auth Token Successfully Read", ConsoleUtil.LogLevel.INFO);
                }
                catch (ExceptionInvalidCallback Exception)
                {
                    Console.WriteLine();
                }
            }
            else
            {
                Twitch.TwitchHandler.Instance.Connect();
                ConsoleUtil.WriteToConsole($"[Twitch Auth] Registered as {Configuration.Handler.Instance.Config.TwitchUsername}", ConsoleUtil.LogLevel.INFO);
            }

            if (Configuration.Handler.Instance.Config.PilotName == null)
            {
                ConsoleUtil.WriteToConsole($"Error, file {Configuration.Handler.Instance.GetConfigFilePath()} is not correctly configured, please configure this file and start the application again.\r\n\r\nPress enter to close this application.", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
                Console.ReadLine();
                _quitAppEvent.Set();
            }

            ConsoleUtil.WriteToConsole($"Attempting to find latest game log file for today for Pilot {Configuration.Handler.Instance.Config.PilotName}", ConsoleUtil.LogLevel.INFO);
            _logWatcher = new Game.LogWatcher();
            bool currentLogFileFound;

            try
            {
                currentLogFileFound = _logWatcher.LocateCurrentLogFile();
                _jitaRun = new Game.JitaRun(_logWatcher);
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole(ex.ToString(), ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
                return;
            }

            if (!currentLogFileFound)
            {
                ConsoleUtil.WriteToConsole("Cannot find the latest log file!", ConsoleUtil.LogLevel.ERROR, ConsoleColor.Red);
                _quitAppEvent.WaitOne();
                return;
            }
            else
            {
                ConsoleUtil.WriteToConsole($"Found game log file {_logWatcher.GetCurrentFileName()}", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
                ConsoleUtil.WriteToConsole("Ready!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
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

            ConsoleUtil.WriteToConsole("\r\n\r\nNotice:\r\nThis application will only detect a new Jita Run from a DOCKED state.\r\nPlease ensure you undock AFTER starting the application.\r\nWhen your ship changes to IN_JITA state you're good to go!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Black, ConsoleColor.Yellow);


            _quitAppEvent.WaitOne();
            _logWatcher.Dispose();
        }

        private void HandleApplicationExitEvent()
        {
            ConsoleUtil.WriteToConsole(" -> Application Exit Event Invoked... Shutting down!",ConsoleUtil.LogLevel.INFO);
            ConsoleUtil.WriteToConsole("Goodbye!",ConsoleUtil.LogLevel.INFO);
            Task.Delay(1000).GetAwaiter().GetResult();
            _quitAppEvent.Set();
        }
    }
}

