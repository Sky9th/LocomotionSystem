using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 噪音事件定义。技能激活时广播，AI 听觉系统消费。
    /// 不是 GameplayEffect——不施加在任何对象上，是瞬时世界广播。
    /// 实际音效由动画事件驱动，不由此资产播放。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Noise", fileName = "Noise_")]
    public sealed class NoiseEventSO : ScriptableObject
    {
        [Header("Noise")]
        [Tooltip("噪音类型。Noise.Combat.WeaponFire / Noise.World.Footstep / Noise.Alert.Voice。AI 行为路由。")]
        public GameplayTagDefinitionSO noiseType;

        [Tooltip("噪音等级。0=无声, 越大传播越远。")]
        public float level;

        [Tooltip("衰减半径（米）。超出此距离 AI 听不到。")]
        public float decayRadius;
    }
}
