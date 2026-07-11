using RedDust.Gameplay.Container;
using RedDust.Gameplay.Properties;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    /// <summary>
    /// 游戏实体数据——所有可持久对象的基类。
    ///
    /// EntityService 是唯一拥有者（Dictionary 注册表），其他系统只持有 Id 或缓存引用。
    /// View 是 Entity 在场景中的 GO 载体——由 EntityService.Spawn 绑定，Despawn 清空。
    /// 无 GO 时 View 为 null（物品在背包、未加载 NPC 等）。
    ///
    /// Preset 即 EntityType——<see cref="CharacterDefSO"/> 为角色，<see cref="ItemDefSO"/> 为物品。
    /// </summary>
    public class Entity
    {
        /// <summary>PropertyTree 通用路径常量。</summary>
        public const string CommonTagsPath = "Common/Tags";

        /// <summary>持久标识。存档/联机稳定引用。</summary>
        public string Id { get; }

        /// <summary>属性预设资产——定义模板、初始值、实体种类。</summary>
        public PropertyPresetSO Preset { get; }

        /// <summary>运行时属性数据。与 Preset 共享同一 PropertyTree 结构。</summary>
        public PropertyTable Properties { get; }

        /// <summary>堆叠数量。1 表示独件（武器、装备），>1 表示合并堆叠（弹药、消耗品）。</summary>
        public int StackCount { get; set; } = 1;

        /// <summary>最大堆叠数——从 PropertyTree 读取。</summary>
        public int MaxStackSize => (int)(Properties?.GetFloat("Common/MaxStackSize") ?? 1);

        /// <summary>堆叠未满——允许同 Preset 物品合并。</summary>
        public bool CanStack => StackCount < MaxStackSize;

        /// <summary>场景中的 GO 载体。无 GO 时为 null。</summary>
        [System.NonSerialized] private GameObject _view;
        public GameObject View { get => _view; internal set => _view = value; }
        public bool HasView => _view != null;

        /// <summary>嵌套容器。容器类实体（背包等）Register 时由 EntityService 自动创建。</summary>
        public RdContainer NestedContainer { get; internal set; }

        /// <summary>命令门面——外部系统通过此模块向此实体下达命令。</summary>
        public EntityCommandModule Command { get; }

        /// <summary>查询门面——外部系统通过此模块读取此实体数据（无需 GO）。</summary>
        public EntityQueryModule Query { get; }

        public Entity(string id, PropertyPresetSO preset)
        {
            Id = id ?? System.Guid.NewGuid().ToString();
            Preset = preset;
            Properties = PropertyTable.FromPreset(preset);
            Command = new EntityCommandModule(this);
            Query = new EntityQueryModule(this);
        }

        /// <summary>每帧驱动属性变化（modifier 衰减等）。由 EntityService 或持有者驱动。</summary>
        public void Tick(float dt) => Properties?.Tick(dt);
    }
}
