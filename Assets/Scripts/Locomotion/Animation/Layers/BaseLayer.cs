using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.State.Layers;
using Game.Locomotion.Config;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers
{
    /// <summary>
    /// Base locomotion layer responsible for idle / walk / run / sprint
    /// style movement based on the locomotion snapshot's gait, posture
    /// and planar speed.
    /// </summary>
    internal sealed class BaseLayer : ILocomotionAnimationLayer
    {
        private const string BaseLayerName = "BaseLocomotion";
        public int LayerIndex => 0;
        public AnimancerLayer Layer { get; set; }

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

        public BaseLayer(AnimancerLayer layer)
        {
            Layer = layer;
        }

        public void Update(in LocomotionAnimationContext context)
        {
            AnimancerStringProfile alias = context.Alias;
            SLocomotion snapshot = context.Snapshot;
            LocomotionAnimationProfile profile = context.Profile;
            LocomotionProfile locomotionProfile = context.LocomotionProfile;
            // Use the actual local planar velocity from the snapshot so
            // that animation directly reflects the intended strafe/forward
            // input in character-local space.
            Vector2 planarVelocity = snapshot.ActualLocalVelocity;

            ELocomotionState stateLayer = snapshot.State;
            StringAsset baseAlias = ResolveBaseLocomotionAlias(stateLayer, snapshot.Gait, alias);
            StringAsset nextAlias = baseAlias;

            bool isAnyTurning = snapshot.IsTurning;

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

            // --- Micro-FSM gating (uninterruptible segments) ---
            bool wantsMove = stateLayer == ELocomotionState.GroundedMoving;

            // While a turn-in-place clip is active, keep it playing until completion.
            // If the user starts moving during the turn, immediately transition into
            // IdleToRun (and lock that too) instead of jumping directly to a gait mixer.
            if (IsTurnInPlaceAlias(lastPlayedAlias, alias))
            {
                if (wantsMove)
                {
                    StringAsset idleToRunAlias = ResolveIdleToRunAliasFromTurn(lastPlayedAlias, alias);
                    if (idleToRunAlias != null)
                    {
                        turnCooldownActive = true;
                        nextAlias = idleToRunAlias;
                    }
                }
                else
                {
                    if (!HasAnimationCompleted(currentState))
                    {
                        nextAlias = lastPlayedAlias;
                    }
                    else
                    {
                        turnCooldownActive = true;
                        nextAlias = baseAlias;
                    }
                }
            }

            // While IdleToRun is playing, do not allow any other animations.
            if (IsIdleToRunAlias(lastPlayedAlias, alias))
            {
                if (!HasAnimationCompleted(currentState))
                {
                    nextAlias = lastPlayedAlias;
                }
                else
                {
                    nextAlias = baseAlias;
                }
            }

            // Keep legacy behaviour for non-turn-in-place turn clips (turn-in-walk/run/sprint):
            // once completed, fall back to the appropriate base locomotion clip even if
            // the higher level locomotion logic still reports turning.
            if (!IsTurnInPlaceAlias(lastPlayedAlias, alias) && IsTurnAlias(lastPlayedAlias, alias) && HasAnimationCompleted(currentState))
            {
                turnCooldownActive = true;
                nextAlias = baseAlias;
            }

            if (nextAlias != null && nextAlias != lastPlayedAlias)
            {
                // Use the Transition Library configuration for fades.
                // When using a Transition Library, the TryPlay(object key)
                // overload is the one that goes through Graph.Transitions.
                currentState = Layer.TryPlay(nextAlias);
                lastPlayedAlias = nextAlias;
            }

            // For grounded movement, update 2D mixer parameter directly,
            // mirroring the legacy adapter behaviour where walkMixer is
            // a Vector2MixerState driven by the local planar velocity.
            if (stateLayer == ELocomotionState.GroundedMoving && currentState is Vector2MixerState vector2Mixer)
            {
                float maxMoveSpeed = locomotionProfile.moveSpeed;
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
            return normalizedTime >= 0.99f;
        }

        private StringAsset ResolveBaseLocomotionAlias(ELocomotionState stateLayer, EMovementGait gait, AnimancerStringProfile alias)
        {
            if (alias == null)
            {
                return null;
            }

            switch (stateLayer)
            {
                case ELocomotionState.GroundedIdle:
                    return alias.idleL;

                case ELocomotionState.GroundedMoving:
                    switch (gait)
                    {
                        case EMovementGait.Walk:
                            return alias.walkMixer;
                        case EMovementGait.Run:
                            return alias.runMixer;
                        case EMovementGait.Sprint:
                            return alias.sprint;
                    }
                    break;
            }

            return null;
        }

        private static bool IsIdleToRunAlias(StringAsset alias, AnimancerStringProfile profile)
        {
            if (profile == null || alias == null)
            {
                return false;
            }

            return alias == profile.idleToRun180L ||
                   alias == profile.idleToRun180R;
        }

        private static bool IsTurnInPlaceAlias(StringAsset alias, AnimancerStringProfile profile)
        {
            if (profile == null || alias == null)
            {
                return false;
            }

            return alias == profile.turnInPlace90L ||
                   alias == profile.turnInPlace90R ||
                   alias == profile.turnInPlace180L ||
                   alias == profile.turnInPlace180R;
        }

        private static StringAsset ResolveIdleToRunAliasFromTurn(StringAsset lastTurnAlias, AnimancerStringProfile profile)
        {
            if (profile == null || lastTurnAlias == null)
            {
                return null;
            }

            bool wasLeftTurn = lastTurnAlias == profile.turnInPlace90L || lastTurnAlias == profile.turnInPlace180L;
            bool wasRightTurn = lastTurnAlias == profile.turnInPlace90R || lastTurnAlias == profile.turnInPlace180R;

            if (wasLeftTurn)
            {
                return profile.idleToRun180L;
            }

            if (wasRightTurn)
            {
                return profile.idleToRun180R;
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

            if (!ShouldEnterTurn(snapshot.IsTurning, turnCooldownActive, absAngle))
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
            bool isRightTurn = angle > 0f;

            switch (snapshot.Gait)
            {
                case EMovementGait.Walk:
                    if (ShouldEnterTurn(snapshot.IsTurning, turnCooldownActive, absAngle))
                    {
                        return isRightTurn ? profile.turnInWalk180R : profile.turnInWalk180L;
                    }
                    break;

                case EMovementGait.Run:
                    if (ShouldEnterTurn(snapshot.IsTurning, turnCooldownActive, absAngle))
                    {
                        return isRightTurn ? profile.turnInRun180R : profile.turnInRun180L;
                    }
                    break;

                case EMovementGait.Sprint:
                    if (ShouldEnterTurn(snapshot.IsTurning, turnCooldownActive, absAngle))
                    {
                        return isRightTurn ? profile.turnInSprint180R : profile.turnInSprint180L;
                    }
                    break;
            }

            return defaultAlias;
        }

        private bool ShouldEnterTurn(bool isTurning, bool cooldownActive, float absAngle)
        {
            if (!isTurning)
            {
                return false;
            }

            if (cooldownActive)
            {
                return false;
            }

            return true;
        }

        private bool IsTurnAlias(StringAsset alias, AnimancerStringProfile profile)
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
