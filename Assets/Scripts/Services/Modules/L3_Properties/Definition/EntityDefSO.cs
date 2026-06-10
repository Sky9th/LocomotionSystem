using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// 实体定义基类。绑定 PropertyTreeSO（结构）与 OverridesJson（变种覆写值）。
    /// 子类追加不属于属性数据的机械规则字段（slots、spawnBehavior 等）。
    /// </summary>
    public abstract class EntityDefSO : ScriptableObject
    {
        [Tooltip("PropertyTreeSO 模板——定义这个实体有哪些属性。")]
        public PropertyTreeSO Template;

        [TextArea(3, 20)]
        [Tooltip("变种覆写 JSON。覆写 Tree 中声明的属性的默认值。格式与 PropertyComponent.OverridesJson 一致。")]
        public string OverridesJson;
    }
}
