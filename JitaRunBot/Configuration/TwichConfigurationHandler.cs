namespace JitaRunBot.Configuration
{
    internal class TwichConfigurationHandler
    {
        public static TwichConfigurationHandler Instance = _instance ??= new TwichConfigurationHandler();
        private static readonly TwichConfigurationHandler? _instance;


        private string RootPath;
        private string FullPath;
        private string FileName = "config.json";

        public TwichConfigurationHandler()
        {
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JitaRunBot");
            FullPath = Path.Combine(RootPath, FileName);

            try
            {
                if (!Directory.Exists(RootPath))
                {
                    Directory.CreateDirectory(RootPath);
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }

            Load();
        }

        public string GetConfigFilePath()
        {
            return FullPath;
        }

        private void Load()
        {

        }

    }
    internal class TwitchConfiguration
    {

    }
}
