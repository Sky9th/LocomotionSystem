using UnityEngine;
using RedDust.Ability;
using RedDust.Entities;

namespace RedDust.Properties
{
    /// <summary>
    /// 属性预设基类。绑定 PropertyTreeSO（结构）与 OverridesJson（变种覆写值）。
    /// 子类追加不属于属性数据的机械规则字段（slots、spawnBehavior 等）。
    /// </summary>
    public abstract class PropertyPresetSO : ScriptableObject
    {
        [Tooltip("PropertyTreeSO 模板——定义这个预设有哪些属性。")]
        public PropertyTreeSO Template;

        [TextArea(3, 20)]
        [Tooltip("变种覆写 JSON。覆写 Tree 中声明的属性的默认值。格式与 PropertyComponent.OverridesJson 一致。")]
        public string OverridesJson;

        [Tooltip("实体 Prefab。EntityService.Spawn 时 Instantiate。")]
        public GameObject Prefab;

        /// <summary>
        /// 从实体实例提取装备伤害效果。
        /// 武器预设（MeleeWeaponSO / RangedWeaponSO 等）覆写。
        /// 非武器预设返回 null。
        /// </summary>
        public virtual EffectSO[] GetDamageEffects(Entity entity) => null;
    }
}
