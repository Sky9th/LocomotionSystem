using Game.Locomotion.Discrete.Core;

namespace Game.Locomotion.Discrete.Coordination
{
    /// <summary>
    /// Default locomotion controller implementation for human-like
    /// characters. Currently delegates entirely to the shared
    /// <see cref="LocomotionGraph"/> without extra rules,
    /// but acts as a clear extension point for future human-specific
    /// posture or condition logic.
    /// </summary>
    internal sealed class HumanLocomotionCoordinator : LocomotionCoordinatorBase
    {
        protected override LocomotionGraph CreateGraph()
        {
            return new LocomotionGraph();
        }
    }
}
