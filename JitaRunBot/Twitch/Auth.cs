namespace JitaRunBot.Twitch
{
    public class Auth
    {
        public string ClientId = "4zj6ekjb1e6xmlv3m4jmzwi7vm3b5m";

        public bool HasTokenExpired()
        {
            return DateTime.Now.ToUniversalTime() >= Configuration.Handler.Instance.Config.TwitchTokenExpiry;
        }

        public bool TestAuth()
        {
            var api = new TwitchLib.Api.TwitchAPI
            {
                Settings =
                {
                    ClientId = ClientId
                }
            };

            var authToken = Configuration.Handler.Instance.Config.TwitchAuthToken;

            if (Configuration.Handler.Instance.Config.TwitchRefreshToken == null ||
                Configuration.Handler.Instance.Config.TwitchTokenExpiry == null ||
                HasTokenExpired())
            {
                Util.Log($"[OAuthChecker] Token has expired, marking it as dirty! (exp: {Configuration.Handler.Instance.Config.TwitchTokenExpiry})", Util.LogLevel.Info);
                return false;
            }

            try
            {
                return api.Auth.ValidateAccessTokenAsync(authToken).WaitAsync(CancellationToken.None).Result != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Validate(bool AutomaticRefresh)
        {
            Util.Log($"[OAuthChecker] Checking OAuth Tokens Validity", Util.LogLevel.Info);
            var validationResult = TestAuth();

            if (!validationResult)
            {
                Util.Log($"[OAuthChecker] Failed to validate OAuth Token",
                    Util.LogLevel.Warn,
                    ConsoleColor.Yellow);

                if (AutomaticRefresh)
                {
                    if (!RefreshToken().GetAwaiter().GetResult())
                    {
                        throw new Exception(
                            $"Unable to refresh Auth Token");
                    }
                    return true;
                }

                return false;
            }
            else
            {
                Util.Log($"[OAuthChecker] Successfully validated OAuth Tokens, Expiry: {Configuration.Handler.Instance.Config.TwitchTokenExpiry}/UTC", Util.LogLevel.Info);
                return true;
            }
        }

        public async Task<bool> RefreshToken()
        {
            // Get our refresh token
            var refreshToken = Configuration.Handler.Instance.Config.TwitchRefreshToken;

            var config = Configuration.Handler.Instance.Config;

            var api = new TwitchLib.Api.TwitchAPI();
            try
            {
                var refreshRequest =
                    await api.Auth.RefreshAuthTokenAsync(refreshToken, Configuration.Handler.Instance.Config.TwitchClientSecret, TwitchHandler.Instance.Auth.ClientId);

                if (refreshRequest != null)
                {

                    config.TwitchAuthToken = refreshRequest.AccessToken;
                    config.TwitchRefreshToken = refreshRequest.RefreshToken;
                    config.TwitchTokenExpiry = DateTime.Now.ToUniversalTime().AddSeconds(refreshRequest.ExpiresIn);

                    Util.Log($"[OAuthRefresh] Successfully refreshed OAuth Tokens", Util.LogLevel.Info);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Util.Log($"[OAuthRefresh] Error while handling an OAuth Token Refresh: {ex.Message}", Util.LogLevel.Error, ConsoleColor.Red);
                return false;
            }

            return false;
        }
    }
}
