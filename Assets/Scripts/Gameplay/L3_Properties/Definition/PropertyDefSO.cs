using RedDust.Core.RdTag;
using System;
using UnityEngine;

namespace RedDust.Gameplay.Properties
{
    public class PropertyDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;

        [TextArea(2, 6)]
        public string Description;

        [HideInInspector]
        public PropertyType Type;

        public bool IsDeprecated;

        /// <summary>
        /// 计算待写入的最终值。子类覆写以处理 raw/default/direct 三种来源的类型专属解析。
        /// 基类默认 passthrough。
        /// </summary>
        public virtual object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            return rawValue;
        }

        /// <summary>
        /// 校验泛型 T 是否与结构体类型匹配。仅 StructPropertyDefSO 覆写。
        /// 基类默认返回 true（不过滤）。
        /// </summary>
        public virtual bool TypeMatches<T>() => true;

        // ============================================================
        // 工厂
        // ============================================================

        /// <summary>工厂：按 PropertyType 创建对应的子类实例。</summary>
        public static PropertyDefSO Create(PropertyType type) => type switch
        {
            PropertyType.Float        => ScriptableObject.CreateInstance<FloatPropertyDefSO>(),
            PropertyType.Int          => ScriptableObject.CreateInstance<IntPropertyDefSO>(),
            PropertyType.Bool         => ScriptableObject.CreateInstance<BoolPropertyDefSO>(),
            PropertyType.String       => ScriptableObject.CreateInstance<StringPropertyDefSO>(),
            PropertyType.RdTag         => ScriptableObject.CreateInstance<RdTagPropertyDefSO>(),
            PropertyType.RdTagList     => ScriptableObject.CreateInstance<RdTagListPropertyDefSO>(),
            PropertyType.AssetRef     => ScriptableObject.CreateInstance<AssetRefPropertyDefSO>(),
            PropertyType.AssetRefList => ScriptableObject.CreateInstance<AssetRefListPropertyDefSO>(),
            PropertyType.Struct       => ScriptableObject.CreateInstance<StructPropertyDefSO>(),
            _ => null
        };
    }
}
