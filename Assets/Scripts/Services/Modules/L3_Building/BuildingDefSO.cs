using UnityEngine;
using RedDust.Properties;

namespace RedDust.Building
{
    /// <summary>
    /// 建筑预设。继承 PropertyPresetSO。
    /// 所有数据全进 PropertyTree（Template + OverridesJson）。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Building", fileName = "NewBuilding")]
    public class BuildingDefSO : PropertyPresetSO
    {
    }
}
