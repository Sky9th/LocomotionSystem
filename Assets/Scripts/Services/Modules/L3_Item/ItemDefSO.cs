using System;
using RedDust.Properties;

namespace RedDust.Items
{
    /// <summary>
    /// ⛔ DEPRECATED — 已拆分为 WeaponDefSO / PropDefSO / SceneItemDefSO / BuildingDefSO。
    /// 保留空壳向后兼容已有 .asset 引用，禁止新建 ItemDefSO。
    /// </summary>
    [Obsolete("ItemDefSO is deprecated. Use WeaponDefSO, PropDefSO, SceneItemDefSO, or BuildingDefSO instead.")]
    public class ItemDefSO : PropertyPresetSO
    {
    }
}
