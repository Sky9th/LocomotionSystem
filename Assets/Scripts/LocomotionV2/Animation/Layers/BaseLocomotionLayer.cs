using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.State.Layers;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers
{
    /// <summary>
    /// Base locomotion layer responsible for idle / walk / run / sprint
    /// style movement based on the locomotion snapshot's gait, posture
    /// and planar speed. The initial version is a stub and will be
    /// implemented in later iterations.
    /// </summary>
    internal sealed class BaseLocomotionLayer : ILocomotionAnimationLayer
    {
        private StringAsset lastPlayedAlias;

        public void Update(in LocomotionAnimationContext context)
        {
            AnimancerStringProfile alias = context.Alias;
            AnimancerComponent animancer = context.Animancer;
            SPlayerLocomotion snapshot = context.Snapshot;
            LocomotionAnimationProfile profile = context.Profile;
            if (animancer == null || alias == null || profile == null)
            {
                return;
            }

            if (animancer.Layers.Count == 0)
            {
                return;
            }

            AnimancerLayer baseLayer = animancer.Layers[0];

            ELocomotionState stateLayer = snapshot.State;
            StringAsset nextAlias = null;

            if (stateLayer == ELocomotionState.GroundedIdle)
            {
                // Idle and turn-in-place.
                float angle = snapshot.TurnAngle;
                float absAngle = Mathf.Abs(angle);
                float exitAngle = profile.turnExitAngle;

                if (snapshot.IsTurning && (exitAngle <= 0f || absAngle > exitAngle))
                {
                    bool isRightTurn = angle > 0f;
                    bool use180 = absAngle > 90f;

                    if (isRightTurn)
                    {
                        // Right turn.
                        StringAsset desired = use180 ? alias.turnInPlace180R : alias.turnInPlace90R;

                        // If we are already playing a 180 turn, do not
                        // downgrade to a 90 turn mid-animation.
                        if (!use180 && lastPlayedAlias == alias.turnInPlace180R)
                        {
                            desired = alias.turnInPlace180R;
                        }

                        nextAlias = desired;
                    }
                    else
                    {
                        // Left turn.
                        StringAsset desired = use180 ? alias.turnInPlace180L : alias.turnInPlace90L;

                        // If we are already playing a 180 turn, do not
                        // downgrade to a 90 turn mid-animation.
                        if (!use180 && lastPlayedAlias == alias.turnInPlace180L)
                        {
                            desired = alias.turnInPlace180L;
                        }

                        nextAlias = desired;
                    }
                }
                else
                {
                    // Plain idle.
                    nextAlias = alias.idleL;
                }
            }
            else if (stateLayer == ELocomotionState.GroundedMoving)
            {
                // TODO: Play walk / run / sprint animation based on gait.
                if (snapshot.Gait == EMovementGait.Walk)
                {
                    nextAlias = alias.walkMixer;
                }
                else if (snapshot.Gait == EMovementGait.Run)
                {
                    nextAlias = alias.runMixer;
                }
                else if (snapshot.Gait == EMovementGait.Sprint)
                {
                    nextAlias = alias.sprintMixer;
                }
            }
            else if (stateLayer == ELocomotionState.Airborne)
            {
                // TODO: Play jump / fall animation based on condition.
            }

            if (nextAlias != null && nextAlias != lastPlayedAlias)
            {
                Logger.Log($"Playing base locomotion animation: {nextAlias},{snapshot.TurnAngle}");
                // Use the Transition Library configuration for fades.
                // When using a Transition Library, the TryPlay(object key)
                // overload is the one that goes through Graph.Transitions.
                baseLayer.TryPlay(nextAlias);
                lastPlayedAlias = nextAlias;
            }
        }
    }
}
