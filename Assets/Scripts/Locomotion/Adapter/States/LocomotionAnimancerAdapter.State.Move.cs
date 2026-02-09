using UnityEngine;
using Animancer.FSM;
using Game.Locomotion.Adapter.Conditions;
using Animancer;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        private sealed class MoveState : State
        {
            private Vector2MixerState currentMoveState;
            private AnimancerState currentTurnState;
            private LocomotionAnimancerAdapter adapter;

            public MoveState(LocomotionAnimancerAdapter adapter) : base(adapter)
            {
                this.adapter = adapter;
                AddTransition(LocomotionConditions.IsStopped, () => Adapter.idleState);
            }

            public override void OnEnterState()
            {
                currentMoveState = (Vector2MixerState)Adapter.baseLayer.TryPlay(Adapter.alias.walkMixer);
            }

            public override void Update()
            {
                if (TryApplyTransitions())
                {
                    return;
                }

                var snapshot = adapter.agent.Snapshot;
                currentMoveState.Parameter = snapshot.LocalVelocity;
                float absAngle = Mathf.Abs(snapshot.TurnAngle);
                if (absAngle > 0f)
                {
                    if (absAngle > 130f)
                    {
                        if (snapshot.TurnAngle > 0f)
                            currentTurnState = adapter.baseLayer.TryPlay(Adapter.alias.turnInWalk180R);
                        else {
                            currentTurnState = adapter.baseLayer.TryPlay(Adapter.alias.turnInWalk180L);
                        }
                    } else {
                        float maxStep = adapter.agent.WalkTurnSpeed * Time.deltaTime;
                        float deltaAngle = Mathf.Sign(snapshot.TurnAngle) * Mathf.Min(maxStep, absAngle);

                        if (Mathf.Abs(deltaAngle) > Mathf.Epsilon)
                        {
                            Logger.Log($"Applying turn delta of {deltaAngle} to model (absAngle={absAngle})");
                            adapter.agent.Model.transform.rotation = Quaternion.AngleAxis(deltaAngle, Vector3.up) * adapter.agent.Model.transform.rotation;
                        }
                    }
                }

                if (currentTurnState != null)
                {
                    if(currentTurnState.NormalizedTime >= 1f - Mathf.Epsilon)
                    {
                        currentMoveState = (Vector2MixerState)Adapter.baseLayer.TryPlay(Adapter.alias.walkMixer);
                        currentTurnState = null;
                    }
                }

                if (absAngle < Mathf.Epsilon) { 
                    currentMoveState = (Vector2MixerState)Adapter.baseLayer.TryPlay(Adapter.alias.walkMixer);
                }
            }
        }
    }
}
