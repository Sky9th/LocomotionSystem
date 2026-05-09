using UnityEngine;
using Game.Audio;

namespace Game.Character.Audio
{
    [CreateAssetMenu(fileName = "CharacterAudioConfig", menuName = "Game/Character/Audio Config")]
    public class CharacterAudioConfigSO : AudioSetSO
    {
        public FootstepSetSO footsteps;
        // 未来: HitReactSetSO, DeathSetSO, BreathSetSO
    }
}
