using UnityEngine;
using RedDust.Audio;
using RedDust.Character.Animation.Drivers;
using RedDust.Character.Animation.Drivers.Locomotion;

namespace RedDust.Character.Audio
{
    public sealed class CharacterAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource footSource;

        private CharacterAudioConfigSO Config =>
            GetComponentInParent<CharacterActor>()?.CharacterAudioConfig;

        private void Start()
        {
            var locoDriver = GetComponentInChildren<LocomotionDriver>();
            if (locoDriver != null)
                locoDriver.BaseLayer.FootstepCallback = OnFootstep;
        }

        private void OnFootstep()
        {
            var config = Config;
            if (config == null || config.footsteps == null) return;
            if (footSource == null) return;

            var request = new AudioRequest("Foot", config.footsteps, AudioChannelType.SFX);
            if (!TryResolve(in request, out var response)) return;

            AudioChannel.Play(in response, footSource);
        }

        private static bool TryResolve(in AudioRequest request, out AudioResponse response)
        {
            if (request.Set is FootstepSetSO footsteps)
            {
                if (footsteps.clip == null) { response = default; return false; }

                var pitch = 1f + Random.Range(-footsteps.pitchVariation, footsteps.pitchVariation);
                response = new AudioResponse(footsteps.clip, footsteps.baseVolume, pitch);
                return true;
            }

            response = default;
            return false;
        }
    }
}
