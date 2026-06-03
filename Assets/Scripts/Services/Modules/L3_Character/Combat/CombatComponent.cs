using UnityEngine;
using RedDust.Core;
using RedDust.Character.Stats;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 角色战斗能力入口。接收一个技能（SkillDefSO）并逐帧执行。
    /// 不关心技能从哪来——槽位/武器映射是角色层的职责。
    ///
    /// Slice 1: 占位构造 + 接口签名，TryActivate 返回 false。
    /// Slice 2: Tick 实现冷却倒计时，TryActivate 接入 CombatPipeline。
    /// </summary>
    public sealed class CombatComponent
    {
        private readonly GameObject _owner;
        private readonly CharacterStats _stats;

        /// <summary>角色当前持有的 GameplayTag 集合。门控/冷却/状态查询共用。</summary>
        public GameplayTagContainer OwnedTags { get; } = new();

        /// <summary>当前激活的技能。CombatDriver（Slice 3）通过此属性读取阶段机配置。</summary>
        internal SkillDefSO ActiveSkill { get; private set; }

        public CombatComponent(GameObject owner, CharacterStats stats)
        {
            _owner = owner;
            _stats = stats;
        }

        /// <summary>
        /// 逐帧更新。Slice 2 在此倒计时 active effects。
        /// </summary>
        public void Tick(float dt)
        {
            // Slice 2: tick active effects, expire cooldowns
        }

        /// <summary>
        /// 尝试激活技能。返回 true 表示技能已执行。
        /// Slice 1 占位，始终返回 false。
        /// </summary>
        /// <param name="skill">要激活的技能定义。调用侧负责从槽位/武器映射解析。</param>
        /// <param name="origin">搜索原点（世界坐标）。</param>
        /// <param name="direction">搜索方向（XZ 平面归一化向量）。</param>
        public bool TryActivate(SkillDefSO skill, Vector3 origin, Vector3 direction)
        {
            // Slice 1 placeholder
            return false;
        }
    }
}
