using UnityEngine;
using RedDust.Container;
using RedDust.Properties;

namespace RedDust.Character
{
    /// <summary>
    /// 角色属性预设。继承 PropertyPresetSO。
    ///
    /// 身体槽位通过 OverridesJson 覆写 Entity 基树的 Common/Slots：
    ///   [{"Path":"Common/Slots","Value":"[{\"SlotId\":\"RightHand\",\"Capacity\":1},...]"}]
    ///
    /// 未覆写时使用 StandardBodySlots 兜底。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDef", menuName = "RedDust/Entity/Character")]
    public class CharacterDefSO : PropertyPresetSO
    {
    }
}
