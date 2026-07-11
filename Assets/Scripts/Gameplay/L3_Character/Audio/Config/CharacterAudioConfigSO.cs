using UnityEngine;
using RedDust.Audio;

namespace RedDust.Character.Audio
{
    [CreateAssetMenu(fileName = "CharacterAudioConfigSO", menuName = "RedDust/Audio/Audio Config")]
    public class CharacterAudioConfigSO : AudioSetSO
    {
        public FootstepSetSO footsteps;
        // 未来: HitReactSetSO, DeathSetSO, BreathSetSO
    }
}
