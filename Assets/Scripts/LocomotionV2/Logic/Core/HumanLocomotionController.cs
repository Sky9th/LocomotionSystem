namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Default locomotion controller implementation for human-like
    /// characters. Currently just wraps the shared
    /// LocomotionStateMachine without additional customisation,
    /// but provides a clear archetype-specific extension point.
    /// </summary>
    internal sealed class HumanLocomotionController : LocomotionControllerBase
    {
        protected override LocomotionStateMachine CreateStateMachine()
        {
            return new LocomotionStateMachine();
        }
    }
}
