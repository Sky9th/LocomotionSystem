using RedDust.Core.Events;
using RedDust.Services.Input;
using UnityEngine;

namespace RedDust.Core.Events
{
    /// <summary>
    /// 技能快捷键事件。单个 Action 多 Binding（Q/W/E/R/T/Y），通过 SButtonInputPayload.BindingIndex 区分槽位。
    /// 替代独立的 InputSkill1Event / InputSkill2Event / ... — 增删技能键只需在 InputActionAsset 里加/删 Binding。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Combat/InputSkillEvent", fileName = "InputSkillEvent")]
    public sealed class InputSkillEvent : GameEvent<SButtonInputPayload> { }
}
