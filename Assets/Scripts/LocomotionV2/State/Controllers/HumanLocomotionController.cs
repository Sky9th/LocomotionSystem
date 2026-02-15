using Game.Locomotion.State.Core;

namespace Game.Locomotion.State.Controllers
{
    /// <summary>
    /// Default locomotion controller implementation for human-like
    /// characters. Currently delegates entirely to the shared
    /// <see cref="LocomotionStateMachine"/> without extra rules,
    /// but acts as a clear extension point for future human-specific
    /// posture or condition logic.
    /// </summary>
    internal sealed class HumanLocomotionController : LocomotionControllerBase
    {
        protected override LocomotionStateMachine CreateStateMachine()
        {
            return new LocomotionStateMachine();
        }
    }
}
