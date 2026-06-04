using UnityEngine;
using RedDust.Core;
using RedDust.Character.Stats;

namespace RedDust.Ability
{
    /// <summary>
    /// 通用能力执行器（MonoBehaviour）。挂 Prefab 上，谁需要谁挂。
    /// 角色、陷阱、Boss 均可使用——不关心使用者是谁。
    ///
    /// Slice 1: 占位，TryActivate 返回 false。
    /// Slice 2: Tick 实现冷却倒计时，TryActivate 接入 AbilityPipeline。
    /// </summary>
    public sealed class AbilityComponent : MonoBehaviour
    {
        [Header("Stats")]
        [Tooltip("角色的属性系统。非角色实体（陷阱/环境）留空。")]
        [SerializeField] private CharacterStats stats;

        /// <summary>当前持有的 GameplayTag 集合。门控/冷却/状态查询共用。</summary>
        public GameplayTagContainer OwnedTags { get; } = new();

        /// <summary>当前激活的能力。AbilityDriver（Slice 3）通过此属性读取阶段机配置。</summary>
        internal AbilityDefSO ActiveSkill { get; private set; }

        private void Awake()
        {
            // stats 从同一 GameObject 获取（如有 CharacterStats 组件）
            if (stats == null)
                stats = GetComponent<CharacterStats>();
        }

        /// <summary>
        /// 逐帧更新。Slice 2 在此倒计时 active effects。
        /// </summary>
        public void Tick(float dt)
        {
            // Slice 2: tick active effects, expire cooldowns
        }

        /// <summary>
        /// 尝试激活能力。返回 true 表示能力已执行。
        /// Slice 1 占位，始终返回 false。
        /// </summary>
        /// <param name="ability">要激活的能力定义。</param>
        /// <param name="origin">搜索原点（世界坐标）。</param>
        /// <param name="direction">搜索方向（XZ 平面归一化向量）。</param>
        public bool TryActivate(AbilityDefSO ability, Vector3 origin, Vector3 direction)
        {
            // Slice 1 placeholder
            return false;
        }
    }
}
