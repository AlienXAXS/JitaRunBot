namespace JitaRunBot
{
    public static class Util
    {
        public static void Log(string message, LogLevel logLevel, ConsoleColor foregroundColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;

            var dtNow = DateTime.Now;

            Console.WriteLine($"[{dtNow.Year:####}-{dtNow.Month:0#}-{dtNow.Day:0#} {dtNow.Hour:0#}:{dtNow.Minute:0#}:{dtNow.Second:0#}-{logLevel}] {message}");            
        }

        public enum LogLevel
        {
            Info,
            Debug,
            Warn,
            Error,
            Fatal
        }
    }
}
