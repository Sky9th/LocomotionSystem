using UnityEngine;
using RedDust.Audio;
using RedDust.Core;
using RedDust.Character.Animation;

namespace RedDust.Character.Audio
{
    public sealed class CharacterAudio : ModuleChildMono
    {
        [SerializeField] private AudioSource footSource;
        private AnimationBrain brain;

        private CharacterAudioConfigSO Config =>
            brain?.BuildContext?.AudioConfig;

        public override void OnAssemble()
        {
            base.OnAssemble();
            brain = GetComponentInChildren<AnimationBrain>();
        }

        public override void OnWire()
        {
            base.OnWire();
            // Footstep 音频延后（俯视角游戏脚步声优先级低，除非机器人等特殊角色）。
            // if (brain != null)
            //     brain.OnFootstep += HandleFootstep;
        }

        private void OnDestroy()
        {
            // Footstep 音频延后。
            // if (brain != null)
            //     brain.OnFootstep -= HandleFootstep;
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
