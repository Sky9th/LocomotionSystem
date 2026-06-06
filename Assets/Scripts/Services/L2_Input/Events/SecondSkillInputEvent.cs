using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 技能槽 2 输入事件。对应 Combat Action Map 的 2ndSkil。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Input/Second Skill Event", fileName = "SecondSkillEventSO")]
    public sealed class SecondSkillInputEventSO : InputEvent<bool>
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