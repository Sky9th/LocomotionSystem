using UnityEngine;

namespace RedDust.Character
{
    [CreateAssetMenu(fileName = "CharacterProfile", menuName = "Game/Character/Character Profile")]
    public sealed class CharacterProfile : ScriptableObject
    {
        [Header("Ground")]
        [Range(0f, 89f)] public float maxGroundSlopeAngle = 55f;
        public LayerMask groundLayerMask = ~0;
        [Min(0f)] public float groundReacquireDebounceDuration;
        public bool enableGroundLocking = true;
        public float groundLockMaxDistance = 0.15f;
        public float groundLockVerticalOffset;

        [Header("Ground Probe")]
        [Min(0.1f)] public float groundProbeHeight = 0.5f;
        [Min(0.1f)] public float groundProbeRadius = 0.25f;

        [Header("Obstacle")]
        public LayerMask obstacleLayerMask = ~0;
        [Min(0f)] public float obstacleProbeVerticalOffset = 0.15f;
        [Min(0f)] public float obstacleProbeDistance = 0.75f;
        [Min(0.1f)] public float obstacleMinClimbHeight = 0.3f;
        [Min(0f)] public float obstacleMaxClimbHeight = 2f;

        [Header("Head Look")]
        [Range(0f, 90f)] public float maxHeadYawDegrees = 75f;
        [Range(0f, 90f)] public float maxHeadPitchDegrees = 75f;
        [Min(0f)] public float headLookRotationSpeed = 1f;
    }
}
