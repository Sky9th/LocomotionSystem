using UnityEngine;
using RedDust.Properties;

namespace RedDust.Character
{
    /// <summary>角色属性预设。继承 PropertyPresetSO，后续追加 Faction、AIProfile 等。</summary>
    [CreateAssetMenu(fileName = "CharacterDef", menuName = "RedDust/Properties/Preset/Character")]
    public class CharacterDefSO : PropertyPresetSO { }
}
