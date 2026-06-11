using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// GameplayTag 的 ScriptableObject 定义资产。
    /// 设计时使用父子引用组织层级，运行时通过隐式转换获取 <see cref="GameplayTag"/> struct。
    ///
    /// 改父级 leafName → 所有子孙 FullTag 自动更新（非字符串副本，安全重命名）。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/GameplayTag", fileName = "Tag_")]
    public sealed class GameplayTagDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("本层级名称片段，不含父级前缀。如 State 的子标签填 Attacking")]
        private string leafName;

        [SerializeField, Tooltip("父级标签 SO。根标签（如 State）为 null")]
        private GameplayTagDefinitionSO parent;

        [SerializeField, Tooltip("标签说明。策划可读的描述文本。")]
        private string description;

        // ── 缓存 ──
        private string cachedFullTag;

        /// <summary>本层级名称片段。</summary>
        public string LeafName => leafName;

        /// <summary>父级标签 SO，根为 null。</summary>
        public GameplayTagDefinitionSO Parent => parent;

        /// <summary>完整层级路径，如 "State.Attacking"。</summary>
        public string FullTag => cachedFullTag;

        /// <summary>层级深度。根=1，"State.Attacking"=2。</summary>
        public int Depth { get; private set; }

        /// <summary>隐式转换到运行时 struct。AbilityDefSO 等持有此 SO 的地方可直接当 GameplayTag 用。</summary>
        public static implicit operator GameplayTag(GameplayTagDefinitionSO def)
            => def != null ? new GameplayTag(def.FullTag) : default;

        private void OnEnable()
        {
            AutoDeriveLeafName();
            RefreshCache();
        }

        private void OnValidate()
        {
            // 编辑器修改 leafName/parent 后立即刷新
            AutoDeriveLeafName();
            RefreshCache();
        }

        private void AutoDeriveLeafName()
        {
            var assetName = name; // ScriptableObject.name = 文件名不含扩展名
            if (string.IsNullOrEmpty(assetName)) return;
            if (!assetName.StartsWith("Tag_")) return;

            var derived = assetName.Substring(4); // "Tag_Species" → "Species"

            if (leafName == derived) return;         // 已正确
            if (string.IsNullOrEmpty(leafName))      // 空 → 自动填
            {
                leafName = derived;
                return;
            }
            // 不匹配 → 修正（覆盖复制粘贴残留值）
            Debug.LogWarning($"[GameplayTag] leafName mismatch: file={assetName}, was=\"{leafName}\", corrected=\"{derived}\"");
            leafName = derived;
        }

        private void RefreshCache()
        {
            cachedFullTag = parent != null
                ? $"{parent.FullTag}.{leafName}"
                : leafName;

            Depth = parent != null ? parent.Depth + 1 : 1;
        }
    }
}
