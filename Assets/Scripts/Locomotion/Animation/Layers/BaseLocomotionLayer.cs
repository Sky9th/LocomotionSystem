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
    /// and planar speed.
    /// </summary>
    internal sealed class BaseLocomotionLayer : ILocomotionAnimationLayer
    {
        private const string BaseLayerName = "BaseLocomotion";

        private StringAsset lastPlayedAlias;

        private AnimancerState currentState;

        private SLocomotionAnimationLayerSnapshot lastSnapshot;

        // Prevent immediately re-entering a turn animation while the
        // higher level locomotion logic still reports "turning" after
        // a turn clip has already completed. This avoids getting stuck
        // in a loop of back-to-back turn clips when the player keeps
        // rotating the camera.
        private bool turnCooldownActive;

        public string LayerName => BaseLayerName;

        public SLocomotionAnimationLayerSnapshot AnimationSnapshot => lastSnapshot;

        public void Update(in LocomotionAnimationContext context)
        {
            AnimancerStringProfile alias = context.Alias;
            AnimancerComponent animancer = context.Animancer;
            SLocomotion snapshot = context.Snapshot;
            LocomotionAnimationProfile profile = context.Profile;
            // Use the actual local planar velocity from the snapshot so
            // that animation directly reflects the intended strafe/forward
            // input in character-local space.
            Vector2 planarVelocity = snapshot.ActualLocalVelocity;

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
            StringAsset baseAlias = GetBaseLocomotionAlias(stateLayer, snapshot.Gait, alias);
            StringAsset nextAlias = baseAlias;

            bool isAnyTurning = snapshot.IsTurningInPlace ||
                                snapshot.IsTurningInWalk ||
                                snapshot.IsTurningInRun ||
                                snapshot.IsTurningInSprint;

            // Once the logical layer stops reporting any turning, we
            // lift the cooldown so a new turn can be started later.
            if (!isAnyTurning)
            {
                turnCooldownActive = false;
            }

            if (stateLayer == ELocomotionState.GroundedIdle)
            {
                StringAsset idleTurnAlias = SelectIdleTurnAlias(in snapshot, alias, profile);
                if (idleTurnAlias != null)
                {
                    nextAlias = idleTurnAlias;
                }
            }
            else if (stateLayer == ELocomotionState.GroundedMoving)
            {
                nextAlias = SelectMovingAlias(in snapshot, alias, profile, baseAlias);
            }
            else if (stateLayer == ELocomotionState.Airborne)
            {
                // TODO: Play jump / fall animation based on condition.
            }

            // If we have just finished a turn animation, immediately
            // fall back to the appropriate base locomotion clip even if
            // the higher level locomotion logic still reports a large
            // turn angle. This prevents the character from getting stuck
            // on the last frame of a turn clip.
            if (IsTurnAlias(lastPlayedAlias, alias) && HasAnimationCompleted(currentState))
            {
                // Mark cooldown so we don't immediately re-enter another
                // turn clip on the next frame while IsTurning* is still
                // being reported by the locomotion state controller.
                turnCooldownActive = true;

                // Fall back to the base locomotion clip for the current
                // locomotion state and gait.
                nextAlias = baseAlias;
            }

            if (nextAlias != null && nextAlias != lastPlayedAlias)
            {
                Logger.Log($"Playing base locomotion animation: {nextAlias}");
                // Use the Transition Library configuration for fades.
                // When using a Transition Library, the TryPlay(object key)
                // overload is the one that goes through Graph.Transitions.
                currentState = baseLayer.TryPlay(nextAlias);
                lastPlayedAlias = nextAlias;
            }

            // For grounded movement, update 2D mixer parameter directly,
            // mirroring the legacy adapter behaviour where walkMixer is
            // a Vector2MixerState driven by the local planar velocity.
            if (stateLayer == ELocomotionState.GroundedMoving && currentState is Vector2MixerState vector2Mixer)
            {
                float maxMoveSpeed = profile.moveSpeed;
                if (maxMoveSpeed > 0f)
                {
                    Vector2 parameter = planarVelocity / maxMoveSpeed;

                    // Clamp magnitude to 1 so it fits the mixer input range.
                    if (parameter.sqrMagnitude > 1f)
                    {
                        parameter.Normalize();
                    }

                    vector2Mixer.Parameter = parameter;
                }
            }

            float normalizedTime = currentState != null ? (float)currentState.NormalizedTime : 0f;
            bool isTurnAnimation = IsTurnAlias(lastPlayedAlias, alias);
            lastSnapshot = new SLocomotionAnimationLayerSnapshot(
                layerName: BaseLayerName,
                alias: lastPlayedAlias,
                normalizedTime: normalizedTime,
                isTurnAnimation: isTurnAnimation);
        }

        private static bool HasAnimationCompleted(AnimancerState state)
        {
            if (state == null)
            {
                return false;
            }

            float normalizedTime = (float)state.NormalizedTime;
            Logger.Log($"Current turn animation normalized time: {normalizedTime}");
            return normalizedTime >= 0.99f;
        }

        private static StringAsset GetBaseLocomotionAlias(ELocomotionState stateLayer, EMovementGait gait, AnimancerStringProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            switch (stateLayer)
            {
                case ELocomotionState.GroundedIdle:
                    return profile.idleL;

                case ELocomotionState.GroundedMoving:
                    switch (gait)
                    {
                        case EMovementGait.Walk:
                            return profile.walkMixer;
                        case EMovementGait.Run:
                            return profile.runMixer;
                        case EMovementGait.Sprint:
                            return profile.sprint;
                    }
                    break;
            }

            return null;
        }

        private StringAsset SelectIdleTurnAlias(in SLocomotion snapshot, AnimancerStringProfile profile, LocomotionAnimationProfile animationProfile)
        {
            if (profile == null || animationProfile == null)
            {
                return null;
            }

            float angle = snapshot.TurnAngle;
            float absAngle = Mathf.Abs(angle);
            float exitAngle = animationProfile.turnExitAngle;

            if (!ShouldEnterTurn(snapshot.IsTurningInPlace, turnCooldownActive, absAngle, exitAngle))
            {
                return null;
            }

            bool isRightTurn = angle > 0f;
            bool use180 = absAngle > 90f;

            if (isRightTurn)
            {
                // Right turn.
                StringAsset desired = use180 ? profile.turnInPlace180R : profile.turnInPlace90R;

                // If we are already playing a 180 turn, do not
                // downgrade to a 90 turn mid-animation.
                if (!use180 && lastPlayedAlias == profile.turnInPlace180R)
                {
                    desired = profile.turnInPlace180R;
                }

                return desired;
            }

            // Left turn.
            StringAsset leftDesired = use180 ? profile.turnInPlace180L : profile.turnInPlace90L;

            // If we are already playing a 180 turn, do not
            // downgrade to a 90 turn mid-animation.
            if (!use180 && lastPlayedAlias == profile.turnInPlace180L)
            {
                leftDesired = profile.turnInPlace180L;
            }

            return leftDesired;
        }

        private StringAsset SelectMovingAlias(in SLocomotion snapshot, AnimancerStringProfile profile, LocomotionAnimationProfile animationProfile, StringAsset defaultAlias)
        {
            if (profile == null || animationProfile == null)
            {
                return defaultAlias;
            }

            float angle = snapshot.TurnAngle;
            float absAngle = Mathf.Abs(angle);
            float exitAngle = animationProfile.turnExitAngle;
            bool isRightTurn = angle > 0f;

            switch (snapshot.Gait)
            {
                case EMovementGait.Walk:
                    if (ShouldEnterTurn(snapshot.IsTurningInWalk, turnCooldownActive, absAngle, exitAngle))
                    {
                        return isRightTurn ? profile.turnInWalk180R : profile.turnInWalk180L;
                    }
                    break;

                case EMovementGait.Run:
                    if (ShouldEnterTurn(snapshot.IsTurningInRun, turnCooldownActive, absAngle, exitAngle))
                    {
                        return isRightTurn ? profile.turnInRun180R : profile.turnInRun180L;
                    }
                    break;

                case EMovementGait.Sprint:
                    if (ShouldEnterTurn(snapshot.IsTurningInSprint, turnCooldownActive, absAngle, exitAngle))
                    {
                        return isRightTurn ? profile.turnInSprint180R : profile.turnInSprint180L;
                    }
                    break;
            }

            return defaultAlias;
        }

        private static bool ShouldEnterTurn(bool isTurning, bool cooldownActive, float absAngle, float exitAngle)
        {
            if (!isTurning)
            {
                return false;
            }

            if (cooldownActive)
            {
                return false;
            }

            if (exitAngle > 0f && absAngle <= exitAngle)
            {
                return false;
            }

            return true;
        }

        private static bool IsTurnAlias(StringAsset alias, AnimancerStringProfile profile)
        {
            if (profile == null || alias == null)
            {
                return false;
            }

            return alias == profile.turnInPlace90L ||
                   alias == profile.turnInPlace90R ||
                   alias == profile.turnInPlace180L ||
                   alias == profile.turnInPlace180R ||
                   alias == profile.turnInWalk180L ||
                   alias == profile.turnInWalk180R ||
                   alias == profile.turnInRun180L ||
                   alias == profile.turnInRun180R ||
                   alias == profile.turnInSprint180L ||
                   alias == profile.turnInSprint180R;
        }
    }
}
