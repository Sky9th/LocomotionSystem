using UnityEngine;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Facade for evaluating the aggregated discrete locomotion state.
    ///
    /// Delegates to an internal ILocomotionController so that all
    /// posture / gait / condition rules are centralised and can be
    /// extended without changing external call sites.
    /// </summary>
    internal static class LocomotionStateController
    {
        private static readonly ILocomotionController Controller = new HumanLocomotionController();

        internal static void Evaluate(
            Vector3 velocity,
            SGroundContact groundContact,
            LocomotionConfigProfile config,
            out SLocomotionDiscreteState discreteState)
        {
            var context = new LocomotionStateContext(velocity, groundContact, config);
            discreteState = Controller.UpdateDiscreteState(in context);
        }
    }
}
