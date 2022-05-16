using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NHttp;


namespace JitaRunBot.Twitch
{
    internal class TwitchCallbackHandler
    {
        private bool _hasRan = false;
        private string _clientId = "4zj6ekjb1e6xmlv3m4jmzwi7vm3b5m";

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
                if (eventArgs.Request.QueryString.AllKeys.Any("access_token".Contains))
                {
                    var code = eventArgs.Request.QueryString["access_token"];
                    Configuration.Handler.Instance.Config.TwitchAuthToken = code;

                    _hasRan = true;
                }
                else
                {
                    if (eventArgs.Request.Path.Equals("/oauth/callback"))
                    {
                        using (var writer = new StreamWriter(eventArgs.Response.OutputStream))
                        {
                            writer.Write(
                                @"<html><head><script language=""javascript"">for(var values,hash=window.location.hash.slice(1),array=hash.split(""&""),form_data={},i=0;i<array.length;i+=1)form_data[(values=array[i].split(""=""))[0]]=values[1];if(void 0!==form_data.access_token){var httpClient=new XMLHttpRequest,url=window.location.href.substring(0,window.location.href.search(""#""))+""?access_token=""+form_data.access_token;httpClient.open(""GET"",url),httpClient.send()}else console.log(""cannot find access_token"");</script></head><body>You may close this window now :)</body></html>");
                        }
                    }
                }

                if (eventArgs.Request.QueryString.AllKeys.Any("error_message".Contains))
                {

                    _hasRan = true;
                }
            };
            
            _httpServer.Start();

            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.FileName = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={_clientId}&redirect_uri=http://localhost:57754/oauth/callback&scope={String.Join("+", _scopes)}";
            myProcess.Start();

            TaskEx.WaitUntil(() => _hasRan).Wait();

            _httpServer.Stop();
        }

        private void LogCallback(string message)
        {
            //ConsoleUtil.WriteToConsole($"TwitchOAuthServer -> {message}", ConsoleUtil.LogLevel.INFO);
        }
    }

    internal class ExceptionInvalidCallback : Exception
    {
        public ExceptionInvalidCallback() { }
        public ExceptionInvalidCallback(string message) : base(message) { }
        public ExceptionInvalidCallback(string message, Exception inner) : base(message, inner) { }
    }
}
