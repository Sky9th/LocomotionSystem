using Animancer;
using Animancer.FSM;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Agent;
using Game.Locomotion.Config;
using System.Collections.Generic;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal enum BaseStateKey
    {
        Idle,
        TurnInPlace,
        TurnInMoving,
        IdleToMoving,
        Moving,
        AirLoop,
        AirLand
    }

    /// <summary>
    /// FSM-driven base locomotion layer.
    ///
    /// Note: This layer is intentionally introduced alongside the legacy implementation
    /// for incremental migration.
    /// </summary>
    internal sealed class BaseLayer : ILocomotionAnimationLayer
    {
        private const string BaseLayerName = "BaseLocomotion";

        public int LayerIndex => 0;
        public AnimancerLayer Layer { get; set; }

        private readonly StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>> stateMachine;
        private readonly BaseIdleState idleState;
        private readonly BaseTurnInPlaceState turnInPlaceState;
        private readonly BaseIdleToMovingState idleToMovingState;
        private readonly BaseMovingState moveState;
        private readonly BaseTurnInMovingState turnInMovingState;
        private readonly BaseAirLoopState airLoopState;
        private readonly BaseAirLandState airLandState;
        private StringAsset lastPlayedAlias;
        private AnimancerState currentState;
        private SLocomotionAnimationLayerSnapshot lastSnapshot;

        // Cached context for states.
        public LocomotionAnimationContext context;

        public string LayerName => BaseLayerName;

        public SLocomotionAnimationLayerSnapshot AnimationSnapshot => lastSnapshot;

        internal LocomotionAliasProfile AliasProfile => context.Alias;
        internal SLocomotion Snapshot => context.Snapshot;
        internal LocomotionProfile LocomotionProfile => context.LocomotionProfile;
        internal LocomotionAnimationProfile AnimationProfile => context.Profile;
        internal float DeltaTime => context.DeltaTime;
        internal ILocomotionModelTransformer ModelRotator => context.ModelRotator;

        public BaseLayer(AnimancerLayer layer)
        {
            Layer = layer;

            idleState = new BaseIdleState(this);
            turnInPlaceState = new BaseTurnInPlaceState(this);
            moveState = new BaseMovingState(this);
            idleToMovingState = new BaseIdleToMovingState(this);
            turnInMovingState = new BaseTurnInMovingState(this);
            airLoopState = new BaseAirLoopState(this);
            airLandState = new BaseAirLandState(this);

            stateMachine = new StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>>();
            stateMachine.Dictionary[BaseStateKey.Idle] = idleState;
            stateMachine.Dictionary[BaseStateKey.TurnInPlace] = turnInPlaceState;
            stateMachine.Dictionary[BaseStateKey.IdleToMoving] = idleToMovingState;
            stateMachine.Dictionary[BaseStateKey.Moving] = moveState;
            stateMachine.Dictionary[BaseStateKey.TurnInMoving] = turnInMovingState;
            stateMachine.Dictionary[BaseStateKey.AirLoop] = airLoopState;
            stateMachine.Dictionary[BaseStateKey.AirLand] = airLandState;
        }

        public void Update(in LocomotionAnimationContext context)
        {
            this.context = context;
            
            EnsureInitialized();

            // The FSM states are responsible for driving transitions.
            stateMachine.CurrentState?.Tick();

            float normalizedTime = currentState != null ? (float)currentState.NormalizedTime : 0f;
            lastSnapshot = new SLocomotionAnimationLayerSnapshot(
                layerName: BaseLayerName,
                alias: lastPlayedAlias,
                normalizedTime: normalizedTime);
        }

        private void EnsureInitialized()
        {
            if (stateMachine.CurrentState != null)
            {
                return;
            }

            stateMachine.ForceSetState(BaseStateKey.Idle, idleState);
        }

        internal bool TrySetState(BaseStateKey key)
        {
            BaseStateKey previousKey = stateMachine.CurrentKey;
            stateMachine.TrySetState(key);
            return !EqualityComparer<BaseStateKey>.Default.Equals(previousKey, stateMachine.CurrentKey);
        }
        
        internal bool ForceSetState(BaseStateKey key)
        {
            BaseStateKey previousKey = stateMachine.CurrentKey;
            stateMachine.ForceSetState(key);
            return !EqualityComparer<BaseStateKey>.Default.Equals(previousKey, stateMachine.CurrentKey);
        }

        internal void PlayFromStart(StringAsset alias)
        {
            if (alias == null)
            {
                return;
            }

            currentState = Layer.TryPlay(alias);
            lastPlayedAlias = alias;

            if (currentState != null)
            {
                currentState.NormalizedTime = 0f;
            }
        }

        internal void PlayIfChanged(StringAsset nextAlias)
        {
            if (nextAlias == null)
            {
                return;
            }

            if (nextAlias == lastPlayedAlias)
            {
                return;
            }

            Play(nextAlias);
        }

        internal void Play(StringAsset alias)
        {
            if (alias == null)
            {
                return;
            }

            currentState = Layer.TryPlay(alias);
            lastPlayedAlias = alias;
        }

        internal bool HasCurrentAnimationCompleted()
        {
            if (currentState == null)
            {
                return false;
            }

            float normalizedTime = (float)currentState.NormalizedTime;
            return normalizedTime >= 0.99f;
        }

    }
}
