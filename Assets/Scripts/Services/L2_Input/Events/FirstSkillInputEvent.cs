using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 技能槽 1 输入事件。对应 Combat Action Map 的 1stSkill。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Input/First Skill Event", fileName = "FirstSkillEventSO")]
    public sealed class FirstSkillInputEventSO : InputEvent<bool>
    {
        protected override void OnPerformed(InputAction.CallbackContext ctx)
        {
            Raise(ctx.ReadValueAsButton());
        }

        protected override void OnCanceled(InputAction.CallbackContext ctx)
        {
            Raise(ctx.ReadValueAsButton());
        }
    }
}