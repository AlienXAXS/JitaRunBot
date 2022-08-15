using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace JitaRunBot.Twitch
{
    public class TwitchHandler : IDisposable
    {
        public static TwitchHandler Instance = _instance ??= new TwitchHandler();
        private static readonly TwitchHandler? _instance;

        public Auth Auth = new();

        TwitchClient _client;

        private bool CheckAuth()
        {
            return Auth.TestAuth();
        }

        public void Connect()
        {

            try
            {
                Auth.Validate(true);
            }
            catch (Exception ex)
            {
                Util.Log(
                    $"[TwitchChat] Unable to connect to TwitchChat.  OAuth Validation failed. Error: {ex.Message}",
                    Util.LogLevel.Error, ConsoleColor.Red);
                return;
            }

            ConnectionCredentials credentials = new ConnectionCredentials(Configuration.Handler.Instance.Config.TwitchUsername, Configuration.Handler.Instance.Config.TwitchAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = null
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);

            _client = new TwitchClient(customClient);
            _client.Initialize(credentials);

            _client.OnJoinedChannel += Client_OnJoinedChannel!;
            _client.OnConnected += Client_OnConnected!;

            _client.OnNoPermissionError += ClientOnOnNoPermissionError;
            _client.OnDisconnected += ClientOnOnDisconnected;

            _client.OnIncorrectLogin += ClientOnOnIncorrectLogin;

            _client.Connect();
        }

        private void ClientOnOnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
        {
            if (_client == null) return;
            if (!Program.AppIsRunning) return;

            Util.Log($"[TwitchChat] Invalid/Expired Auth Credentials", Util.LogLevel.Error, ConsoleColor.Red);

            if (_client.IsConnected) _client.Disconnect();

            if (!Auth.TestAuth())
            {
                Util.Log("[TwitchChat] Attemtping a token refresh", Util.LogLevel.Info);
                if (Auth.RefreshToken().GetAwaiter().GetResult())
                {
                    Util.Log($"[TwitchChat] Token refresh successful, reconnecting to TwitchChat", Util.LogLevel.Info);
                    SetCredentials();
                    _client?.Connect();
                }
                else
                {
                    Util.Log("[TwitchChat] Unable to refresh TwitchChat Token", Util.LogLevel.Error, ConsoleColor.Red);
                }
            }
        }

        private void SetCredentials()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Configuration.Handler.Instance.Config.TwitchUsername, Configuration.Handler.Instance.Config.TwitchAuthToken);
            _client?.SetConnectionCredentials(credentials);
        }

        private void ClientOnOnDisconnected(object? sender, OnDisconnectedEventArgs e)
        {
            if (Program.AppIsRunning)
            {
                if (!CheckAuth())
                    SetCredentials();
                
                _client.Reconnect();
            }
        }

        private void ClientOnOnNoPermissionError(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Util.Log($"[Twitch Bot] Connected to Twitch IRC", Util.LogLevel.Info);
            _client.JoinChannel("mind1");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Util.Log($"[Twitch Bot] Joined channel {e.Channel}", Util.LogLevel.Info);

            SendChatMessage("[JitaRunBot] Sucessfully connected to twitch!");
        }

        public void SendChatMessage(string msg)
        {
            _client?.SendMessage("mind1", msg);
        }

        public void Dispose()
        {
            _client.Disconnect();

            var maxTries = 10;
            var currentTry = 1;
            while (_client.IsConnected)
            {
                Util.Log($"Attempting to disconnect from Twitch Chat IRC [{currentTry}/{maxTries}]", Util.LogLevel.Info);
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