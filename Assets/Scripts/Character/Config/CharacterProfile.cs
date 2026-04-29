using UnityEngine;

namespace Game.Character.Config
{
    [CreateAssetMenu(fileName = "CharacterProfile", menuName = "Game/Character/Character Profile")]
    public sealed class CharacterProfile : ScriptableObject
    {
        [Header("Ground")]
        [Range(0f, 89f)] public float maxGroundSlopeAngle = 55f;
        public LayerMask groundLayerMask = ~0;
        public Vector3 groundStandBoxHalfExtents = new(0.15f, 0.03f, 0.15f);
        [Min(0f)] public float groundRayLength = 10f;
        [Min(0f)] public float groundReacquireDebounceDuration;
        public bool enableGroundLocking = true;
        public float groundLockVerticalOffset;
        public float groundDetectVerticalOffset = 0.01f;

        [Header("Obstacle")]
        public LayerMask obstacleLayerMask = ~0;
        [Min(0f)] public float obstacleProbeVerticalOffset = 0.15f;
        [Min(0f)] public float obstacleProbeDistance = 0.75f;
        [Min(0f)] public float obstacleMaxClimbHeight = 2f;

        [Header("Head Look")]
        [Range(0f, 90f)] public float maxHeadYawDegrees = 75f;
        [Range(0f, 90f)] public float maxHeadPitchDegrees = 75f;
        [Min(0f)] public float headLookRotationSpeed = 1f;
    }
}
