namespace RedDust.Shared
{
    public sealed class LogChannel
    {
        public string Name { get; }
        public LogLevel Threshold { get; set; }

        internal LogChannel(string name, LogLevel threshold)
        {
            Name = name;
            Threshold = threshold;
        }

        public bool IsEnabled(LogLevel level)
        {
            if (level >= LogLevel.Error) return true;
            if (level < Threshold) return false;
            if (level < LogManager.GlobalThreshold) return false;
            return true;
        }

        public LogChannel GetChild(string childName)
        {
            return LogManager.GetChannel($"{Name}.{childName}", Threshold);
        }

        public void Trace(string message)   => Emit(LogLevel.Trace, message);
        public void Debug(string message)   => Emit(LogLevel.Debug, message);
        public void Info(string message)    => Emit(LogLevel.Info, message);
        public void Warning(string message) => Emit(LogLevel.Warning, message);
        public void Error(string message)   => Emit(LogLevel.Error, message);
        public void Fatal(string message)   => Emit(LogLevel.Fatal, message);

        public void Always(string message) => LogManager.EmitInternal(LogLevel.Info, Name, message, bypass: true);

        private void Emit(LogLevel level, string message)
        {
            if (!IsEnabled(level)) return;
            LogManager.EmitInternal(level, Name, message, bypass: false);
        }
    }
}
