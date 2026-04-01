using UnityEngine;
using Game.Locomotion.Discrete.Core;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Config;
using Game.Locomotion.Input;
using Game.Locomotion.Computation;

namespace Game.Locomotion.Discrete.Core
{
    /// <summary>
    /// Base implementation of <see cref="ILocomotionCoordinator"/>.
    ///
    /// Owns a <see cref="LocomotionGraph"/> and forwards
    /// evaluation requests to it. Concrete archetype controllers
    /// provide a graph instance via the base constructor.
    /// </summary>
    internal abstract class LocomotionCoordinatorBase : ILocomotionCoordinator
    {
        protected readonly LocomotionGraph Graph;
        protected readonly LocomotionTraversalGraph TraversalGraph;
        protected readonly LocomotionTurningGraph TurningGraph;

        private SLocomotionDiscrete currentState;

        protected LocomotionCoordinatorBase(
            LocomotionGraph graph,
            LocomotionTraversalGraph traversalGraph,
            LocomotionTurningGraph turningGraph)
        {
            Graph = graph;
            TraversalGraph = traversalGraph;
            TurningGraph = turningGraph;

            // Safe default state before the first evaluation.
            currentState = SLocomotionDiscrete.Default;
        }

        public SLocomotionDiscrete CurrentState => currentState;
        public SLocomotionTraversal CurrentTraversal => TraversalGraph.CurrentTraversal;

        public ELocomotionPhase CurrentPhase => currentState.Phase;
        public EPosture CurrentPosture => currentState.Posture;
        public EMovementGait CurrentGait => currentState.Gait;
        public ELocomotionCondition CurrentCondition => currentState.Condition;

        public SLocomotionDiscrete Evaluate(
            in SLocomotionMotor motor,
            LocomotionProfile profile,
            in SLocomotionInputActions actions,
            float deltaTime)
        {
            currentState = Graph.Evaluate(in motor, in actions);
            SLocomotionDiscrete discrete = currentState;
            SLocomotionTraversal traversal = TraversalGraph.Evaluate(in motor, in actions, in discrete, deltaTime);

            switch (traversal.Stage)
            {
                case ELocomotionTraversalStage.Committed:
                    currentState = SLocomotionDiscrete.CreateActionControlled(in discrete);
                    break;

                default:
                    bool isTurning = TurningGraph.Evaluate(
                        motor.TurnAngle,
                        motor.LocomotionHeading,
                        profile,
                        deltaTime,
                        in discrete);

                    currentState = new SLocomotionDiscrete(
                        discrete.Phase,
                        discrete.Posture,
                        discrete.Gait,
                        discrete.Condition,
                        isTurning);
                    break;
            }

            return currentState;
        }

        // Note: Previously we had overridable hooks (CreateGraph/EvaluateDiscreteState).
        // With the current single-archetype setup those were redundant, so the
        // graph is injected via the constructor and evaluation is inlined.
    }
}
