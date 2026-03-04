using Game.Locomotion.Discrete.Aspects;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Input;

namespace Game.Locomotion.Discrete.Core
{
    /// <summary>
    /// Composes the individual locomotion state layers into a single
    /// discrete locomotion snapshot.
    ///
    /// The initial version only drives Phase and Gait, while keeping
    /// Posture and Condition at fixed, safe defaults. Additional
    /// layers can be added incrementally without changing external
    /// callers.
    /// </summary>
    internal sealed class LocomotionGraph
    {
        private readonly ILocomotionAspect<ELocomotionPhase> phaseAspect;
        private readonly ILocomotionAspect<EMovementGait> gaitAspect;
        private readonly ILocomotionAspect<EPosture> postureAspect;

        private SLocomotionDiscrete currentState;

        public LocomotionGraph()
        {
            phaseAspect = new PhaseAspect();
            gaitAspect = new GaitAspect();
            postureAspect = new PostureAspect();

            Reset();
        }

        /// <summary>Latest evaluated discrete locomotion state.</summary>
        public SLocomotionDiscrete CurrentState => currentState;

        public void Reset()
        {
            phaseAspect.Reset(ELocomotionPhase.GroundedIdle);
            gaitAspect.Reset(EMovementGait.Idle);
            postureAspect.Reset(EPosture.Standing);

            currentState = new SLocomotionDiscrete(
                ELocomotionPhase.GroundedIdle,
                EPosture.Standing,
                EMovementGait.Idle,
                ELocomotionCondition.Normal,
                isTurning: false);
        }

        /// <summary>
        /// Evaluate all layers for the given context and return the
        /// aggregated discrete locomotion state.
        /// </summary>
        public SLocomotionDiscrete Evaluate(in SLocomotionAgent agent, in SLocomotionInputActions actions)
        {
            phaseAspect.Update(in agent, in actions);
            gaitAspect.Update(in agent, in actions);
            postureAspect.Update(in agent, in actions);

            ELocomotionPhase phase = phaseAspect.Current;
            EMovementGait gait = gaitAspect.Current;
            EPosture posture = postureAspect.Current;
            ELocomotionCondition condition = ELocomotionCondition.Normal;

            currentState = new SLocomotionDiscrete(phase, posture, gait, condition, isTurning: false);
            return currentState;
        }
    }
}
