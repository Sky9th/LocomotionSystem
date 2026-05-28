namespace Game.Utility.Logging
{
    public interface ILogAppender
    {
        LogLevel Threshold { get; }
        void Append(LogLevel level, string channel, string message);
    }
}
