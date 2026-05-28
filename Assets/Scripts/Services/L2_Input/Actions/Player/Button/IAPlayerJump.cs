using RedDust.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.Input
{
    [CreateAssetMenu(menuName = "Inputs/Player/IA Player Jump")]
    public class IAPlayerJump : InputActionHandler
    {
        private static LogChannel _log;

        protected override void Execute(InputAction.CallbackContext context)
        {
            if (!IsEnabled) return;

            if (_log == null) _log = LogManager.GetChannel(nameof(IAPlayerJump));
            _log.Debug("Jump input received.");

            bool rawInput = context.ReadValueAsButton();
            SIActionJump intent = SIActionJump.CreateEvent(rawInput, context.phase);
            eventDispatcher.Publish(intent);
        }
    }
}
