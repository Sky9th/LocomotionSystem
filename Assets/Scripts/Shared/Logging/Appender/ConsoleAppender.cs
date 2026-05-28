namespace RedDust.Shared
{
    public sealed class ConsoleAppender : ILogAppender
    {
        public LogLevel Threshold { get; }

        public ConsoleAppender(LogLevel threshold = LogLevel.Trace)
        {
            Threshold = threshold;
        }

        public void Append(LogLevel level, string channel, string message)
        {
            if (level < Threshold) return;

            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    Logger.Log(message, channel);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(message, channel);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Logger.LogError(message, channel);
                    break;
            }
        }
    }
}
