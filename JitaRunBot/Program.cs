namespace JitaRunBot
{
    public class Program
    {
        private bool appIsRunning = true;
        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);
        private Game.LogWatcher _logWatcher;
        private Game.JitaRun _jitaRun;

        public static void Main()
        {
            ConsoleUtil.WriteToConsole("AGN JitaRun Bot Starting Up", ConsoleUtil.LogLevel.INFO, ConsoleColor.White);
            new Program().MainMethod();
        }

        public void MainMethod()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => HandleApplicationExitEvent();
            Console.CancelKeyPress += (sender, args) => HandleApplicationExitEvent();

            if ( Configuration.Handler.Instance.Config.PilotName == null )
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
                ConsoleUtil.WriteToConsole("Nope", ConsoleUtil.LogLevel.ERROR, ConsoleColor.Red);
            }
            else
            {
                ConsoleUtil.WriteToConsole($"Found game log file {_logWatcher.GetCurrentFileName()}", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
                ConsoleUtil.WriteToConsole("Ready!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
            }

            DiscordWebHookHandler.Instance.SendDiscordMessage($"JitaRunBot started v0.3 - Configuration loaded OK!\r\nPilot: {Configuration.Handler.Instance.Config.PilotName}");
            DiscordWebHookHandler.Instance.SendDiscordMessage($"Waiting for {Configuration.Handler.Instance.Config.PilotName} to undock from Jita.");

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

