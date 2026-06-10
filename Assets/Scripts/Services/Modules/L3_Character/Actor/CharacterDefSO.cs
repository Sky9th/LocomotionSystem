using UnityEngine;
using RedDust.Properties;

namespace RedDust.Character
{
    /// <summary>角色属性定义。继承 EntityDefSO，后续追加 Faction、AIProfile 等。</summary>
    [CreateAssetMenu(fileName = "CharacterDef", menuName = "RedDust/Entity Def/Character")]
    public class CharacterDefSO : EntityDefSO { }
}
