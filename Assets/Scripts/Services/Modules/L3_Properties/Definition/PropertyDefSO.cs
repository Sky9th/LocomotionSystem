using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "PropertyDefinition", menuName = "RedDust/Properties/Property Definition")]
    public class PropertyDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;

        [TextArea(2, 6)]
        public string Description;

        public PropertyType Type;

        public bool IsDeprecated;

        [Header("Float")]
        public float Min;

        public float Max = 100f;
        public float DefaultFloat = 100f;

        [Header("Int")]
        public int MinInt;

        public int MaxInt = 100;
        public int DefaultInt;

        [Header("Bool")]
        public bool DefaultBool;

        [Header("String")]
        public string DefaultString;

        [Header("AssetRef")]
        public string DefaultAssetGUID;

        /// <summary>
        /// 约束可拖拽的 Unity 资产类型，如 "UnityEngine.Sprite" 或 "UnityEngine.GameObject"。
        /// 空 = 不限制。
        /// </summary>
        public string AssetTypeConstraint;

        [Header("Struct")]
        [Tooltip("仅 Struct 类型有效。C# struct 全限定名，如 \"RedDust.Items.SlotDef\"。")]
        public string StructTypeName;

        [TextArea(2, 5)]
        [Tooltip("仅 Struct 类型有效。默认值 JSON。空数组 = \"[]\"。")]
        public string DefaultStructJson;
    }
}
