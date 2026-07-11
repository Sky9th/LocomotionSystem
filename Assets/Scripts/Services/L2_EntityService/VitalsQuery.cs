using RedDust.Gameplay.Character;
using RedDust.Gameplay.Properties;

namespace RedDust.Services.EntityService
{
    /// <summary>
    /// 生理特征查询——L1 层，始终可用。
    ///
    /// 封装 PropertyTable 的 string path 访问，提供类型安全的属性读取。
    /// Max 值从 PropertyTable.GetMax(path) 读取（每个 FloatProperty 自带 Min/Max）。
    /// </summary>
    public readonly struct VitalsQuery
    {
        private readonly PropertyTable _props;

        public float HP => _props?.GetFloat(CharacterConst.PropertyPath.Vitals.HP) ?? 0f;
        public float MaxHP => _props?.GetMax(CharacterConst.PropertyPath.Vitals.HP) ?? 0f;
        public float HpRatio => MaxHP > 0f ? HP / MaxHP : 0f;
        public bool IsAlive => HP > 0f;

        public float Hunger => _props?.GetFloat(CharacterConst.PropertyPath.Vitals.Hunger) ?? 0f;
        public float MaxHunger => _props?.GetMax(CharacterConst.PropertyPath.Vitals.Hunger) ?? 0f;

        // Future（PropertyTree 扩展后启用）:
        // public float Thirst => _props?.GetFloat(CharacterConst.PropertyPath.Vitals.Thirst) ?? 0f;
        // public float Stamina => _props?.GetFloat(CharacterConst.PropertyPath.Vitals.Stamina) ?? 0f;

        internal VitalsQuery(PropertyTable props) { _props = props; }
    }
}
