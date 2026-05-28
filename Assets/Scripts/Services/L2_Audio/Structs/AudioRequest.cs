namespace Game.Audio
{
    public readonly struct AudioRequest
    {
        public string Key { get; }
        public AudioSetSO Set { get; }
        public AudioChannelType Channel { get; }

        public AudioRequest(string key, AudioSetSO set, AudioChannelType channel)
        {
            Key = key;
            Set = set;
            Channel = channel;
        }

        public static AudioRequest None => default;
    }
}
