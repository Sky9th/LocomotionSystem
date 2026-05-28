using UnityEngine;

namespace RedDust.Audio
{
    public static class AudioChannel
    {
        public static void Play(in AudioResponse response, AudioSource source)
        {
            if (!response.IsValid || source == null) return;

            source.pitch = response.Pitch;
            source.PlayOneShot(response.Clip, response.Volume);
        }

        public static void Play(in AudioResponse response, Vector3 worldPosition)
        {
            if (!response.IsValid) return;

            AudioSource.PlayClipAtPoint(response.Clip, worldPosition, response.Volume);
        }
    }
}
