using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// Global configuration entry point for the game.
    /// This will gradually become the central place for cross-system tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "GameProfile", menuName = "RedDust/Core/Game Profile")]
    public sealed class GameProfile : ScriptableObject
    {
        [Header("Camera")]
        [Min(0f)] public float cameraLookRotationSpeed = 1f;
    }
}
