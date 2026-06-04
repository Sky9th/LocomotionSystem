using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 噪音事件载荷。每次技能激活时发布一次（非每命中目标）。
    /// 敌人 AI 订阅此事件，用于听觉感知（Phase 4.2）。
    /// </summary>
    public readonly struct SNoiseEvent
    {
        /// <summary>噪音源世界坐标。</summary>
        public readonly Vector3 SourcePosition;

        /// <summary>噪音传播半径。</summary>
        public readonly float Radius;

        /// <summary>噪音等级（0-6）。</summary>
        public readonly float Level;

        /// <summary>噪音类型标签。</summary>
        public readonly GameplayTag NoiseType;

        /// <summary>噪音源 GameObject。便于 AI 区分玩家 / 环境。</summary>
        public readonly GameObject SourceObject;

        public SNoiseEvent(Vector3 sourcePosition, float radius, float level, GameplayTag noiseType, GameObject sourceObject)
        {
            SourcePosition = sourcePosition;
            Radius = radius;
            Level = level;
            NoiseType = noiseType;
            SourceObject = sourceObject;
        }

        public static SNoiseEvent None => default;
    }
}
