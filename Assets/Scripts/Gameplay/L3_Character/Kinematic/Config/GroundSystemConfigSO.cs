using UnityEngine;

namespace RedDust.Gameplay.Character.Kinematic
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

        [Header("Movement Formula")]
        [Min(0f)] public float agilitySpeedBonus = 0.03f;
        [Range(0f, 1f)] public float weightPenaltyRatio = 0.2f;

        [Header("Combat Formula")]
        [Tooltip("每点力量增加伤害百分比。0.05 = +5%。")]
        [Min(0f)] public float strengthDamageBonus = 0.05f;

        // TODO: 世界规则类配置不应放 Character SO 中。当前暂存于此，
        // 等地图/天气/难度等系统就位后统一迁移到世界级配置。
    }
}
