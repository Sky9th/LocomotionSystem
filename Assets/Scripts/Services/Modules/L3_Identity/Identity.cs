using UnityEngine;
using RedDust.Core;

namespace RedDust
{
    /// <summary>
    /// 实体身份。回答"这个 GameObject 在游戏世界里是谁"。
    /// 持有身份标签（物种、阵营、角色类型），供过滤、AI、UI 等系统查询。
    ///
    /// 与 AbilityExecutor 的分离：Identity 是"身份"（永久属性），
    /// AbilityExecutor 是"能力"（技能执行）。平民 NPC 只有前者，战斗单位两者都有。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Identity : MonoBehaviour
    {
        /// <summary>实体持有的标签集合。物种、阵营、身份等。</summary>
        public GameplayTagContainer Tags { get; } = new();

        [Header("Identity")]
        [Tooltip("初始身份标签。运行时可通过 Tags 增删。")]
        [SerializeField] private GameplayTagDefinitionSO[] initialTags;

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
