using JNogueira.Discord.Webhook.Client;

namespace JitaRunBot
{
    public class DiscordWebHookHandler
    {
        public static DiscordWebHookHandler Instance = _instance ?? (_instance = new DiscordWebHookHandler());
        private static DiscordWebHookHandler _instance;

        private DiscordWebhookClient discordWebhookClient;

        public DiscordWebHookHandler()
        {
            discordWebhookClient = new DiscordWebhookClient(Configuration.Handler.Instance.Config.DiscordWebHookUrl);
        }

        public void SendDiscordMessage(string message)
        {
            new Thread((ThreadStart)async delegate
            {
                var whMessage = new DiscordMessage(message);
                await discordWebhookClient.SendToDiscord(whMessage);
            }).Start();
        }
    }
}
