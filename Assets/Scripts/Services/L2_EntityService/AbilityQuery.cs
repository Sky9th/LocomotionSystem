namespace RedDust.Entities
{
    /// <summary>
    /// 技能查询（L3）—— 封装 AbilityExecutor + AbilityForest 只读访问。
    ///
    /// 参照 EquipmentQuery 模式，纯数据门面，不做写操作。
    /// 由 PlayerService 在角色 Start 完成后通过 entity.Query.Ability 设置。
    /// 非角色 Entity 为 null。
    /// </summary>
    public class AbilityQuery
    {
        private readonly Ability.AbilityExecutor _executor;
        private readonly Ability.AbilityForest _forest;

        /// <summary>当前可用的主动技能列表。</summary>
        public Ability.ActiveAbilitySO[] ActiveAbilities =>
            _forest?.ResolvedActives ?? System.Array.Empty<Ability.ActiveAbilitySO>();

        /// <summary>获取技能的剩余冷却时间（秒）。不在冷却中返回 0。</summary>
        public float GetCooldownRemaining(Ability.ActiveAbilitySO ability)
        {
            if (_executor == null || ability == null) return 0f;
            return _executor.GetAbilityCooldownRemaining(ability);
        }

        /// <summary>指定技能是否正在管道中执行。</summary>
        public bool IsActive(Ability.ActiveAbilitySO ability)
        {
            if (_executor == null || ability == null) return false;
            var pipeline = _executor.Pipeline;
            return pipeline != null && !pipeline.IsIdle && pipeline.Context.Ability == ability;
        }

        internal AbilityQuery(Ability.AbilityExecutor executor, Ability.AbilityForest forest)
        {
            _executor = executor;
            _forest = forest;
        }
    }
}
