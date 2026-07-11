using System;

namespace RedDust.Properties
{
    /// <summary>
    /// 标记一个 struct 可用于 PropertyType.Struct 属性。
    /// 不加此标记的 struct 不会出现在 Editor 下拉框中。
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class PropertyStructAttribute : Attribute { }
}
