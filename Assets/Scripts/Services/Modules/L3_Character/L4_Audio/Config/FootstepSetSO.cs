using UnityEngine;
using RedDust.Audio;

namespace RedDust.Character.Audio
{
    [CreateAssetMenu(fileName = "FootstepSet", menuName = "RedDust/Character/Audio/Footstep Set")]
    public class FootstepSetSO : AudioSetSO
    {
        public AudioClip clip;

        [Header("Settings")]
        [Range(0f, 1f)] public float baseVolume = 0.8f;
        [Range(0f, 0.5f)] public float pitchVariation;
    }
}
