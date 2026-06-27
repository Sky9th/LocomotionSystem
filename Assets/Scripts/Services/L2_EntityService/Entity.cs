using RedDust.Properties;

namespace RedDust.Entities
{
    /// <summary>
    /// 游戏实体数据——所有可持久对象的基类。
    ///
    /// 纯 C#，不依赖 GameObject。EntityService 是唯一拥有者（Dictionary 注册表），
    /// 其他系统（Actor、Container、地面 GO）只持有 Id 或缓存引用，不存在多份拷贝。
    ///
    /// Preset 即 EntityType——<see cref="CharacterDefSO"/> 为角色，<see cref="ItemDefSO"/> 为物品。
    /// </summary>
    public class Entity
    {
        /// <summary>持久标识。存档/联机稳定引用。</summary>
        public string Id { get; }

        /// <summary>属性预设资产——定义模板、初始值、实体种类。</summary>
        public PropertyPresetSO Preset { get; }

        /// <summary>运行时属性数据。与 Preset 共享同一 PropertyTree 结构。</summary>
        public PropertyTable Properties { get; }

        public Entity(string id, PropertyPresetSO preset)
        {
            Id = id ?? System.Guid.NewGuid().ToString();
            Preset = preset;
            Properties = PropertyTable.FromPreset(preset);
        }

        /// <summary>每帧驱动属性变化（modifier 衰减等）。由 EntityService 或持有者驱动。</summary>
        public void Tick(float dt) => Properties?.Tick(dt);
    }
}
