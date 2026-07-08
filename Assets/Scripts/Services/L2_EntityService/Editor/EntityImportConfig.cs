#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace RedDust.Entities.Editor
{
    /// <summary>
    /// 统一 Entity DTO — 替代各类别独立的 XxxEntry。
    /// entityType 用于多类型类别（Weapon: "MeleeWeapon"|"RangedWeapon", Prop: "Armor"|"Consumable"|...）。
    /// 单类型类别（Character/Building/SceneItem）为 null。
    /// </summary>
    [Serializable]
    public class EntityEntry
    {
        public string entityType;     // 类型标签，单类型类别为 null
        public string name;
        public string templateName;   // PropertyTreeSO.name
        public string overridesJson;
        public string prefabGuid;
    }

    /// <summary>
    /// 统一 Export 文件格式。
    /// entities 替代原来各不相同的 weapons/props/characters/buildings/items 字段名。
    /// </summary>
    [Serializable]
    public class EntityExportFile
    {
        public string version = "1.0";
        public string description;
        public string category;           // "Character"|"Weapon"|"Prop"|"Building"|"SceneItem"
        public EntityEntry[] entities;
    }

    /// <summary>
    /// Import/Export 配置。承载各类别之间的所有差异。
    /// </summary>
    public class EntityImportConfig
    {
        /// <summary>类别显示名，如 "Weapon"</summary>
        public string Category;

        /// <summary>面包屑，如 "L3_Weapon · JSON ↔ .asset"</summary>
        public string Breadcrumb;

        /// <summary>资产根目录，如 "Assets/Data/Entities/Weapons"</summary>
        public string DataRoot;

        /// <summary>AssetDatabase 过滤器，如 "t:WeaponDefSO"</summary>
        public string AssetFilter;

        /// <summary>默认导出文件名（不含扩展名），如 "weapons_export"</summary>
        public string DefaultFileName;

        /// <summary>类型映射表。单类型类别为 null。key = entityType 标签, value = C# Type</summary>
        public Dictionary<string, Type> TypeMap;

        /// <summary>默认创建类型。TypeMap 为空时使用。</summary>
        public Type DefaultType;

        /// <summary>Preview 委托。接收 filePath，返回 HTML 格式的预览文本。</summary>
        public Func<string, string> BuildPreview;
    }
}
#endif
