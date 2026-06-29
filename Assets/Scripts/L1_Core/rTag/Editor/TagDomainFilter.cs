#if UNITY_EDITOR
namespace RedDust.Core.Editor
{
    /// <summary>
    /// Tag 域过滤器 — 集中定义每个 Tag 字段应该选择哪个根下的标签。
    /// 所有 Editor 调用 TagPicker 时引用此处的常量，Tag 结构变动只需改这一个文件。
    /// </summary>
    public static class TagDomainFilter
    {
        // ── 域常量 ──
        public const string ABILITY_DEFINITION = "Ability.Definition";
        public const string ABILITY_DAMAGE     = "Ability.Damage";
        public const string ABILITY_EFFECT     = "Ability.Effect";
        public const string ABILITY_IMPACT     = "Ability.Impact";
        public const string ABILITY_TREE       = "Ability.Tree";
        public const string ABILITY_EXECUTE    = "Ability.Execute";
        public const string IDENTITY           = "Identity";
        public const string ENTITY_WEAPON      = "Entity.Weapon";
        public const string ENTITY_ITEM        = "Entity.Item";
        public const string BODY               = "Body";
        public const string GRIP               = "Grip";
        public const string GRIP_MELEE         = "Grip.Melee";
        public const string GRIP_RANGED        = "Grip.Ranged";
        public const string NOISE              = "Noise";

        // ── 字段映射 ──
        public const string ABILITY_TAG                = ABILITY_DEFINITION;
        public const string SHARED_COOLDOWN_TAG        = ABILITY_DEFINITION;
        public const string EXTRA_EXCLUSION_TAGS       = ABILITY_DEFINITION;
        public const string TARGET_REQUIRED_TAG        = IDENTITY;
        public const string TREE_TAGS                  = ABILITY_TREE;
        public const string COMPATIBLE_WEAPON_TAGS     = ENTITY_WEAPON;
        public const string COMPATIBLE_GRIP_TAGS       = GRIP;
        public const string EFFECT_TAG_DAMAGE          = ABILITY_DAMAGE;
        public const string EFFECT_TAG_IMPACT          = ABILITY_IMPACT;
        public const string EFFECT_TAG_EFFECT          = ABILITY_EFFECT;
        public const string APPLICATION_BLOCKED_TAGS   = ABILITY_EFFECT;
        public const string GRANTED_TAGS               = ABILITY_EFFECT;
        public const string NOISE_TYPE                 = NOISE;
        public const string EXCLUSION_ROOTS            = ABILITY_DEFINITION;
        public const string INITIAL_TAGS               = IDENTITY;
        public const string GRIP_TAG                   = GRIP;
    }
}
#endif
