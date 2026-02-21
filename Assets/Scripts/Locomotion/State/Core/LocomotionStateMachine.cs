using Game.Locomotion.State.Layers;

namespace Game.Locomotion.State.Core
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
    internal sealed class LocomotionStateMachine
    {
        private readonly ILocomotionStateLayer<ELocomotionState> phaseLayer;
        private readonly ILocomotionStateLayer<EMovementGait> gaitLayer;
        private readonly ILocomotionStateLayer<EPostureState> postureLayer;

        private SLocomotionDiscreteState currentState;

        public LocomotionStateMachine()
        {
            phaseLayer = new PhaseStateLayer();
            gaitLayer = new GaitStateLayer();
            postureLayer = new PostureStateLayer();

            Reset();
        }

        /// <summary>Latest evaluated discrete locomotion state.</summary>
        public SLocomotionDiscreteState CurrentState => currentState;

        public void Reset()
        {
            phaseLayer.Reset(ELocomotionState.GroundedIdle);
            gaitLayer.Reset(EMovementGait.Idle);
            postureLayer.Reset(EPostureState.Standing);

            currentState = new SLocomotionDiscreteState(
                ELocomotionState.GroundedIdle,
                EPostureState.Standing,
                EMovementGait.Idle,
                ELocomotionCondition.Normal);
        }

        /// <summary>
        /// Evaluate all layers for the given context and return the
        /// aggregated discrete locomotion state.
        /// </summary>
        public SLocomotionDiscreteState Evaluate(in LocomotionStateContext context)
        {
            phaseLayer.Update(in context);
            gaitLayer.Update(in context);
            postureLayer.Update(in context);

            ELocomotionState phase = phaseLayer.Current;
            EMovementGait gait = gaitLayer.Current;
            EPostureState posture = postureLayer.Current;
            ELocomotionCondition condition = ELocomotionCondition.Normal;

            currentState = new SLocomotionDiscreteState(phase, posture, gait, condition);
            return currentState;
        }
    }
}
