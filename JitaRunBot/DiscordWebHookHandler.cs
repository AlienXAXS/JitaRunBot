using JNogueira.Discord.Webhook.Client;

namespace JitaRunBot
{
    public class DiscordWebHookHandler
    {
        public static DiscordWebHookHandler Instance = _instance ??= new DiscordWebHookHandler();
        private static DiscordWebHookHandler _instance;

        private DiscordWebhookClient discordWebhookClient;

        public DiscordWebHookHandler()
        {
            if (IsConfigured())
                discordWebhookClient = new DiscordWebhookClient(Configuration.Handler.Instance.Config.DiscordWebHookUrl);
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(Configuration.Handler.Instance.Config.DiscordWebHookUrl);
        }

        public DiscordWebhookClient GetDiscordWebHook()
        {
            return discordWebhookClient;
        }
    }
}
