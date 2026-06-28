using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust
{
    /// <summary>
    /// 实体身份——回答"这个 GameObject 在游戏世界里是谁"。
    ///
    /// 双重身份模型：
    ///   EntityId — 数据身份，对应 EntityService 注册表中的 Entity.Id。存档/联机引用锚点。
    ///   Tags     — 设计身份（物种、阵营、角色类型），供过滤、AI、UI 等系统查询。
    ///
    /// 与 AbilityExecutor 的分离：Identity 是"身份"（永久属性），
    /// AbilityExecutor 是"能力"（技能执行）。平民 NPC 只有前者，战斗单位两者都有。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Identity : MonoBehaviour
    {
        /// <summary>数据身份 — EntityService 中的唯一标识。为空表示尚未绑定 Entity。</summary>
        public string EntityId => _entityId;
        [SerializeField] private string _entityId;

        /// <summary>实体持有的设计标签集合。物种、阵营、身份等。</summary>
        public GameplayTagContainer Tags { get; } = new();

        [Header("Identity")]
        [Tooltip("初始身份标签。运行时可通过 Tags 增删。")]
        [SerializeField] private GameplayTagDefinitionSO[] initialTags;

        /// <summary>Entity 的运行时属性。由 EntityService.Spawn 在 BindEntity 之后注入。</summary>
        internal PropertyTable Properties { get; private set; }

        /// <summary>绑定 Entity 数据。由 EntityService.Spawn 调用。</summary>
        internal void BindEntity(string entityId)
        {
            _entityId = entityId;
        }

        /// <summary>注入 Entity 的 PropertyTable 并合并 Common/Tags 到身份标签。由 EntityService.Spawn 调用。</summary>
        internal void SetProperties(PropertyTable properties)
        {
            Properties = properties;
            var entityTags = properties?.GetTagList("Common/Tags");
            if (entityTags != null)
            {
                foreach (var tag in entityTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                        Tags.AddTag(tag);
                }
            }
        }

        private void Awake()
        {
            if (initialTags == null) return;
            foreach (var tag in initialTags)
            {
                if (tag != null) Tags.AddTag(tag.FullTag);
            }
        }
    }
}
