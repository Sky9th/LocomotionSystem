using UnityEngine;
using RedDust.Gameplay.Properties;

namespace RedDust.Gameplay.SceneItem
{
    /// <summary>
    /// 场景物品预设（家具/装饰物/场景物体）。
    /// 完整 PropertyTree 系统，未来支持可破坏/可拾取/可燃烧等交互属性。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Scene Item", fileName = "NewSceneItem")]
    public class SceneItemDefSO : PropertyPresetSO
    {
    }
}
