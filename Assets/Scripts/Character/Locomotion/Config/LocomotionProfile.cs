using UnityEngine;

namespace Game.Character.Locomotion
{
    [CreateAssetMenu(fileName = "LocomotionProfile", menuName = "Game/Character/Locomotion Profile")]
    public sealed class LocomotionProfile : ScriptableObject
    {
        [Header("Motion")]
        [Min(0f)] public float moveSpeed = 4f;
        [Min(0f)] public float acceleration = 5f;

        [Header("Abilities")]
        public bool canSprint = true;
        public bool canCrouch = true;
        public bool canProne = true;

        [Header("Turning")]
        [Range(0f, 180f)] public float turnEnterAngle = 65f;
        [Range(0f, 25f)] public float turnCompletionAngle = 5f;
        [Range(0f, 45f)] public float lookStabilityAngle = 2f;
        [Min(0f)] public float lookStabilityDuration = 0.15f;
    }
}
