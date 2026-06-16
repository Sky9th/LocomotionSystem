using UnityEngine;
using RedDust.Audio;
using RedDust.Core;
using RedDust.Character.Animation;

namespace RedDust.Character.Audio
{
    public sealed class CharacterAudio : ModuleComponent
    {
        [SerializeField] private AudioSource footSource;

        private CharacterAudioConfigSO Config =>
            GetComponentInParent<CharacterActor>()?.CharacterAudioConfig;

        public override void OnWire()
        {
            base.OnWire();
            var brain = GetComponentInChildren<AnimationBrain>();
            if (brain != null)
                brain.OnFootstep += HandleFootstep;
        }

        private void HandleFootstep()
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
