using RedDust.Core.GameService;
using UnityEngine;

namespace RedDust.Core.GameService
{
    /// <summary>
    /// Global configuration entry point for the game.
    /// This will gradually become the central place for cross-system tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "GameProfileSO", menuName = "RedDust/Core/Game Profile")]
    public sealed class GameProfileSO : ScriptableObject
    {
        [Header("Camera")]
        [Min(0f)] public float cameraLookRotationSpeed = 1f;
    }
}
