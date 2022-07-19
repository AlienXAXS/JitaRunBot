using System;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace JitaRunBot.Twitch
{
    public class TwitchHandler : IDisposable
    {
        public static TwitchHandler Instance = _instance ??= new TwitchHandler();
        private static readonly TwitchHandler? _instance;

        TwitchClient client;

        public void Connect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Configuration.Handler.Instance.Config.TwitchUsername, Configuration.Handler.Instance.Config.TwitchAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, "Mind1");

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;

            client.Connect();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            //Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Connected to Twitch IRC", ConsoleUtil.LogLevel.INFO);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Joined channel {e.Channel}", ConsoleUtil.LogLevel.INFO);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {

        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {

        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {

        }

        public void Dispose()
        {
            client.Disconnect();

            var maxTries = 10;
            var currentTry = 1;
            while (client.IsConnected)
            {
                ConsoleUtil.WriteToConsole($"Attempting to disconnect from Twitch Chat IRC [{currentTry}/{maxTries}]", ConsoleUtil.LogLevel.INFO);
                Thread.Sleep(1000);
                ++currentTry;

                if (currentTry > maxTries)
                {
                    return;
                }
            }
        }
    }
}