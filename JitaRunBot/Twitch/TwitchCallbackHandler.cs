using System.Diagnostics;
using System.Globalization;
using System.Net;
using NHttp;


namespace JitaRunBot.Twitch
{
    internal class TwitchCallbackHandler
    {
        private bool _hasRan = false;

        private HttpServer _httpServer;
        private readonly List<string> _scopes = new List<string>()
            { "chat:edit", "chat:read" };


        public void GetTwitchToken()
        {
            var myProcess = new Process();

            _httpServer = new HttpServer();
            _httpServer.EndPoint = new IPEndPoint(IPAddress.Loopback, 57754);

            _httpServer.RequestReceived += async (server, eventArgs) =>
            {
                if (eventArgs.Request.QueryString.AllKeys.Any("access_token".Contains!))
                {
                    var accessToken = eventArgs.Request.QueryString["access_token"];
                    var refreshToken = eventArgs.Request.QueryString["refresh_token"];
                    var expiresIn = eventArgs.Request.QueryString["expires_in"];

                    if ( long.TryParse(expiresIn, NumberStyles.Any, new DateTimeFormatInfo(), out var expiresInLong))
                    {
                        var dtEpoch = DateTime.Now.ToUniversalTime().AddSeconds(expiresInLong);

                        Configuration.Handler.Instance.Config.TwitchAuthToken = accessToken;
                        Configuration.Handler.Instance.Config.TwitchRefreshToken = refreshToken;
                        Configuration.Handler.Instance.Config.TwitchTokenExpiry = dtEpoch;

                        _hasRan = true;
                    }
                }
                else
                {
                    if (eventArgs.Request.Path.Equals("/oauth/callback"))
                    {
                        await using var writer = new StreamWriter(eventArgs.Response.OutputStream);
                        await writer.WriteAsync(@"<html><head><script language=""javascript"">for(var values,hash=window.location.hash.slice(1),array=hash.split(""&""),form_data={},i=0;i<array.length;i+=1)form_data[(values=array[i].split(""=""))[0]]=values[1];if(void 0!==form_data.access_token){var httpClient=new XMLHttpRequest,url=window.location.href.substring(0,window.location.href.search(""#""))+""?access_token=""+form_data.access_token;httpClient.open(""GET"",url),httpClient.send()}else console.log(""cannot find access_token"");</script></head><body>You may close this window now :)</body></html>");
                    }
                }

                if (eventArgs.Request.QueryString.AllKeys.Any("error_message".Contains))
                {
                    _hasRan = true;
                }
            };
            
            _httpServer.Start();

            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.FileName = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={TwitchHandler.Instance.Auth.ClientId}&redirect_uri=http://localhost:57754/oauth/callback&scope={String.Join("+", _scopes)}";
            myProcess.Start();

            TaskEx.WaitUntil(() => _hasRan).Wait();

            _httpServer.Stop();
        }
    }

    internal class ExceptionInvalidCallback : Exception
    {
        public ExceptionInvalidCallback() { }
        public ExceptionInvalidCallback(string message) : base(message) { }
        public ExceptionInvalidCallback(string message, Exception inner) : base(message, inner) { }
    }
}
