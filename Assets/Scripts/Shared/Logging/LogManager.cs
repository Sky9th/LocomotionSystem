using System.Collections.Generic;

namespace Game.Utility.Logging
{
    public static class LogManager
    {
        public static LogLevel GlobalThreshold { get; set; } = LogLevel.Info;

        private static readonly Dictionary<string, LogChannel> channels = new();
        private static readonly List<ILogAppender> appenders = new();

        static LogManager()
        {
            appenders.Add(new ConsoleAppender(LogLevel.Trace));
        }

        public static LogChannel GetChannel(string name, LogLevel? defaultThreshold = null)
        {
            if (channels.TryGetValue(name, out var existing))
                return existing;

            var threshold = defaultThreshold ?? GlobalThreshold;
            var channel = new LogChannel(name, threshold);
            channels[name] = channel;
            return channel;
        }

        public static void AddAppender(ILogAppender appender)
        {
            appenders.Add(appender);
        }

        public static void RemoveAppender(ILogAppender appender)
        {
            appenders.Remove(appender);
        }

        internal static void EmitInternal(LogLevel level, string channel, string message, bool bypass)
        {
            foreach (var appender in appenders)
            {
                if (bypass || level >= appender.Threshold)
                    appender.Append(level, channel, message);
            }
        }
    }
}
