using Animancer;
using Animancer.FSM;
using UnityEngine;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Kinematic;
using RedDust.Character.Locomotion;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseLayer
    {
        private readonly StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>> fsm;
        private readonly CharacterBuildContext _buildContext;
        private readonly LocomotionAnimationSetSO _defaultAnimSet;
        private LocomotionAnimationSetSO _lastAnimSet;
        private bool _lastWasIdle;
        private SCharacterFrameContext ctx;
        private float deltaTime;
        private ITransition lastPlayedTransition;
        private AnimancerState currentAnimState;

        internal LocomotionAnimationSetSO AnimSet { get; private set; }

        /// <summary>非 null 时 IdleState 使用此 clip，忽略 AnimSet.idleL。Partial grip 静止时使用。</summary>
        internal ITransition IdleOverride { get; private set; }
        internal LocomotionAnimationConfigSO AnimProfile { get; }
        internal CharacterRig Rig => _buildContext?.Rig;
        internal SCharacterFrameContext Ctx => ctx;
        internal float DeltaTime => deltaTime;
        internal AnimancerLayer Layer { get; }

        internal float AirborneStartY;
        internal float MaxFallDistance;

        internal System.Action FootstepCallback;
        private AnimancerState injectedMixer;

        internal BaseLayer(AnimancerLayer layer, LocomotionAnimationSetSO defaultAnimSet,
            LocomotionAnimationConfigSO animProfile, CharacterBuildContext buildContext)
        {
            _buildContext = buildContext;
            Layer = layer;
            _defaultAnimSet = defaultAnimSet;
            AnimSet = defaultAnimSet;
            AnimProfile = animProfile;
            fsm = new StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>>();
            fsm.Dictionary[BaseStateKey.Idle] = new BaseIdleState(this);
            fsm.Dictionary[BaseStateKey.Moving] = new BaseMovingState(this);
            fsm.Dictionary[BaseStateKey.TurnInPlace] = new BaseTurnInPlaceState(this);
            fsm.Dictionary[BaseStateKey.AirLoop] = new BaseAirLoopState(this);
            fsm.Dictionary[BaseStateKey.AirLand] = new BaseAirLandState(this);
        }

        internal void Update(SCharacterFrameContext ctx, float dt)
        {
            this.ctx = ctx;
            deltaTime = dt;

            // 每帧自决：根据 grip/gait 切换 AnimSet 和 IdleOverride
            EvaluateAnimSet();

            if (fsm.CurrentState == null) fsm.ForceSetState(BaseStateKey.Idle);
            if (Rig == null)
                Debug.LogError($"[BaseLayer] Rig null during Update — buildContext not set.");
            fsm.CurrentState?.Tick();
        }

        // TODO: Gait vs Phase 时序不一致——Gait 松手立刻变 Idle，Phase 等 velocity 归零才变。
        // Partial grip 减速期间 Arm 淡出 + FullBody 未切 idle，导致武器姿态短暂消失。
        private void EvaluateAnimSet()
        {
            var animSet = _buildContext?.ResolvedLocoAnimSet ?? _defaultAnimSet;
            if (animSet == null) return;
            bool isIdle = ctx.Discrete.Gait == EMovementGait.Idle;

            if (animSet == _lastAnimSet && isIdle == _lastWasIdle) return;
            _lastAnimSet = animSet;
            _lastWasIdle = isIdle;

            if (animSet.HasFullLocomotion)
            {
                AnimSet = animSet;
                IdleOverride = null;
            }
            else
            {
                AnimSet = _defaultAnimSet;
                IdleOverride = isIdle ? animSet?.idleL : null;
            }
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

        // TODO: InjectFootstepEvents 写法有多个隐患——
        //   1. 硬编码 0.12f/0.62f 魔法数字，脚步时间应来自 AnimProfile 或动画事件配置
        //   2. injectedMixer 只防重复不防陈旧：动画集切换后旧 mixer 引用仍保留，事件可能已失效
        //   3. events.Add 只增不删，无清理机制；长时运行可能积累事件
        //   4. child.Events(this, out) 把 BaseLayer 当 key 的设计耦合了 Animancer 扩展细节，可读性差
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
