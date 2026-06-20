using Animancer;
using Animancer.FSM;
using UnityEngine;
using RedDust.Character;
using RedDust.Character.Animation;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseLayer
    {
        private readonly StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>> fsm;
        private readonly CharacterBuildContext _buildContext;
        private CharacterFrameContext ctx;
        private float deltaTime;
        private ITransition lastPlayedTransition;
        private AnimancerState currentAnimState;

        internal LocomotionAnimationSetSO AnimSet { get; set; }
        internal LocomotionAnimationConfigSO AnimProfile { get; }
        internal CharacterRig Rig => _buildContext?.Rig;  // 实时读取，Model 替换自动更新
        internal CharacterFrameContext Ctx => ctx;
        internal float DeltaTime => deltaTime;
        internal AnimancerLayer Layer { get; }

        internal float AirborneStartY;
        internal float MaxFallDistance;

        internal System.Action FootstepCallback;
        private AnimancerState injectedMixer;

        internal BaseLayer(AnimancerLayer layer, LocomotionAnimationSetSO animSet, LocomotionAnimationConfigSO animProfile,
            CharacterBuildContext buildContext)
        {
            _buildContext = buildContext;
            Layer = layer;
            AnimSet = animSet;
            AnimProfile = animProfile;
            fsm = new StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>>();
            fsm.Dictionary[BaseStateKey.Idle] = new BaseIdleState(this);
            fsm.Dictionary[BaseStateKey.Moving] = new BaseMovingState(this);
            fsm.Dictionary[BaseStateKey.TurnInPlace] = new BaseTurnInPlaceState(this);
            fsm.Dictionary[BaseStateKey.AirLoop] = new BaseAirLoopState(this);
            fsm.Dictionary[BaseStateKey.AirLand] = new BaseAirLandState(this);
        }

        internal void Update(CharacterFrameContext ctx, float dt)
        {
            this.ctx = ctx;
            deltaTime = dt;
            if (fsm.CurrentState == null) fsm.ForceSetState(BaseStateKey.Idle);
            if (Rig == null)
                Debug.LogError($"[BaseLayer] Rig null during Update — buildContext not set.");
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

        internal bool HasCompleted()
            => currentAnimState != null && currentAnimState.NormalizedTime >= 0.99f;

        internal bool ApplyTurnStepRotation()
        {
            if (Rig == null || AnimProfile == null) return false;

            var absAngle = Mathf.Abs(Ctx.Motor.TurnAngle);
            if (absAngle < 0.5f) return false;

            var gait = Ctx.Discrete.Gait;
            var speed = AnimProfile.GetTurnSpeed(Ctx.Discrete.Posture, gait, gait != EMovementGait.Idle);
            if (speed <= 0f) return false;

            var step = Mathf.Min(speed * DeltaTime, absAngle);
            var delta = Mathf.Sign(Ctx.Motor.TurnAngle) * step;
            Rig.ApplyRotation(Quaternion.AngleAxis(delta, Vector3.up));
            return true;
        }

        internal void InvalidateAnimationCache()
        {
            lastPlayedTransition = null;
        }

        internal void Play(ITransition transition)
        {
            if (transition == null) return;
            lastPlayedTransition = transition;
            currentAnimState = Layer.Play(transition);
            InjectFootstepEvents();
        }

        internal void PlayIfChanged(ITransition transition)
        {
            if (transition == null || ReferenceEquals(transition, lastPlayedTransition)) return;
            lastPlayedTransition = transition;
            currentAnimState = Layer.Play(transition);
            InjectFootstepEvents();
        }

        private void InjectFootstepEvents()
        {
            if (currentAnimState == null || currentAnimState == injectedMixer) return;

            var mixer = currentAnimState as MixerState<Vector2>;
            if (mixer == null) return;

            injectedMixer = mixer;

            for (int i = 0; i < mixer.ChildCount; i++)
            {
                var child = mixer.GetChild(i);
                if (child == null) continue;

                if (child.Events(this, out var events))
                {
                    events.Add(0.12f, () => FootstepCallback?.Invoke());
                    events.Add(0.62f, () => FootstepCallback?.Invoke());
                }
            }
        }
    }
}
