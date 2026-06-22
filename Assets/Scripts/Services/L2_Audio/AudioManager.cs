using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Audio
{
    public enum AudioChannelType
    {
        Master,
        Music,
        SFX,
        Ambience,
        UI,
        Voice,
        Alert
    }

    public class AudioManager : ModuleChildMono
    {
        private readonly Dictionary<AudioChannelType, float> channelVolumes = new()
        {
            { AudioChannelType.Master, 1f },
            { AudioChannelType.Music, 1f },
            { AudioChannelType.SFX, 1f },
            { AudioChannelType.Ambience, 1f },
            { AudioChannelType.UI, 1f },
            { AudioChannelType.Voice, 1f },
            { AudioChannelType.Alert, 1f },
        };

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
        }

        public float MasterVolume => channelVolumes[AudioChannelType.Master];

        public void SetChannelVolume(AudioChannelType channel, float volume)
        {
            channelVolumes[channel] = Mathf.Clamp01(volume);
        }

        public float GetChannelVolume(AudioChannelType channel)
        {
            return channelVolumes.TryGetValue(channel, out var v) ? v : 1f;
        }

        public void MuteChannel(AudioChannelType channel)
        {
            channelVolumes[channel] = 0f;
        }

        public void SetMasterVolume(float volume)
        {
            channelVolumes[AudioChannelType.Master] = Mathf.Clamp01(volume);
        }
    }
}
