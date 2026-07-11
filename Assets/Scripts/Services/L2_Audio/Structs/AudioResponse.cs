using UnityEngine;

namespace RedDust.Services.Audio
{
    public readonly struct AudioResponse
    {
        public AudioClip Clip { get; }
        public float Volume { get; }
        public float Pitch { get; }

        public AudioResponse(AudioClip clip, float volume, float pitch)
        {
            Clip = clip;
            Volume = volume;
            Pitch = pitch;
        }

        public bool IsValid => Clip != null;
        public static AudioResponse None => default;
    }
}
