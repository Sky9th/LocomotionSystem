using Animancer;
using Animancer.FSM;
using UnityEngine;
using Game.Character.Components;
using Game.Character.Locomotion;
using Game.Locomotion.Animation.Config;

namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseLayer
    {
        private readonly StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>> fsm;
        private SCharacterSnapshot snapshot;
        private float deltaTime;
        private StringAsset lastPlayedAlias;
        private AnimancerState currentAnimState;

        internal LocomotionAliasProfile Alias { get; }
        internal LocomotionAnimationProfile AnimProfile { get; }
        internal LocomotionProfile LocoProfile { get; }
        internal CharacterRig Rig { get; }
        internal SCharacterSnapshot Snapshot => snapshot;
        internal float DeltaTime => deltaTime;
        internal AnimancerLayer Layer { get; }

        internal BaseLayer(AnimancerLayer layer, LocomotionAliasProfile alias, LocomotionAnimationProfile animProfile,
            LocomotionProfile locoProfile, CharacterRig rig)
        {
            Layer = layer;
            Alias = alias;
            AnimProfile = animProfile;
            LocoProfile = locoProfile;
            Rig = rig;
            fsm = new StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>>();
            fsm.Dictionary[BaseStateKey.Idle] = new BaseIdleState(this);
            fsm.Dictionary[BaseStateKey.Moving] = new BaseMovingState(this);
            fsm.Dictionary[BaseStateKey.TurnInPlace] = new BaseTurnInPlaceState(this);
            fsm.Dictionary[BaseStateKey.IdleToMoving] = new BaseIdleToMovingState(this);
            fsm.Dictionary[BaseStateKey.TurnInMoving] = new BaseTurnInMovingState(this);
            fsm.Dictionary[BaseStateKey.AirLoop] = new BaseAirLoopState(this);
            fsm.Dictionary[BaseStateKey.AirLand] = new BaseAirLandState(this);
        }

        internal void Update(SCharacterSnapshot snap, float dt)
        {
            snapshot = snap;
            deltaTime = dt;
            if (fsm.CurrentState == null) fsm.ForceSetState(BaseStateKey.Idle);
            fsm.CurrentState?.Tick();
        }

        internal bool TrySetState(BaseStateKey key)
        {
            var prev = fsm.CurrentKey;
            fsm.TrySetState(key);
            return !System.Collections.Generic.EqualityComparer<BaseStateKey>.Default.Equals(prev, fsm.CurrentKey);
        }

        internal bool ForceSetState(BaseStateKey key)
        {
            var prev = fsm.CurrentKey;
            fsm.ForceSetState(key);
            return !System.Collections.Generic.EqualityComparer<BaseStateKey>.Default.Equals(prev, fsm.CurrentKey);
        }

        internal void PlayFromStart(StringAsset alias)
        {
            if (alias == null) return;
            Play(alias);
            if (currentAnimState != null) currentAnimState.NormalizedTime = 0f;
        }

        internal bool HasCompleted()
            => currentAnimState != null && currentAnimState.NormalizedTime >= 0.99f;

        internal bool ApplyTurnStepRotation()
        {
            if (Rig == null || AnimProfile == null) return false;

            var absAngle = Mathf.Abs(Snapshot.Locomotion.Motor.TurnAngle);
            if (absAngle <= Mathf.Epsilon) return false;

            var gait = Snapshot.Locomotion.Discrete.Gait;
            var speed = AnimProfile.GetTurnSpeed(Snapshot.Locomotion.Discrete.Posture, gait, gait != EMovementGait.Idle);
            if (speed <= 0f) return false;

            var step = Mathf.Min(speed * DeltaTime, absAngle);
            var delta = Mathf.Sign(Snapshot.Locomotion.Motor.TurnAngle) * step;
            Rig.ApplyRotation(Quaternion.AngleAxis(delta, Vector3.up));
            return true;
        }

        internal void InvalidateAnimationCache()
        {
            lastPlayedAlias = null;
        }

        internal void Play(StringAsset alias)
        {
            if (alias == null) return;
            currentAnimState = Layer.TryPlay(alias);
            lastPlayedAlias = alias;
        }

        internal void PlayIfChanged(StringAsset alias)
        {
            if (alias == null || alias == lastPlayedAlias) return;
            Play(alias);
        }
    }
}
