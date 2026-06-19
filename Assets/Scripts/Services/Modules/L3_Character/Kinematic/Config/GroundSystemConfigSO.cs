using UnityEngine;

namespace RedDust.Character.Kinematic
{
    /// <summary>
    /// 地面/障碍物探测与锁地参数——物理系统调参，与角色本身无关。
    /// 所有角色共享同一份配置（世界级定义），不随角色变化。
    /// </summary>
    [CreateAssetMenu(fileName = "GroundSystemConfigSO", menuName = "RedDust/Character/Ground System Config")]
    public sealed class GroundSystemConfigSO : ScriptableObject
    {
        [Header("Ground Probe")]
        [Min(0.1f)] public float probeHeight = 0.5f;
        [Min(0.1f)] public float probeRadius = 0.25f;

        [Header("Ground Layer")]
        public LayerMask groundLayerMask = ~0;

        [Header("Obstacle Layer")]
        public LayerMask obstacleLayerMask = ~0;

        [Header("Ground Lock")]
        public bool enableGroundLocking = true;
        public float groundLockMaxDistance = 0.15f;
        public float groundLockVerticalOffset;

        [Header("Debounce")]
        [Min(0f)] public float groundReacquireDebounceDuration;
    }
}
