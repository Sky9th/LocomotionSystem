using Animancer;
using Animancer.FSM;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Config;
using Game.Locomotion.State.Layers;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal enum BaseStateKey
    {
        Idle,
        TurnInPlace,
        IdleToMoving,
        Moving,
    }

    /// <summary>
    /// FSM-driven base locomotion layer.
    ///
    /// Note: This layer is intentionally introduced alongside the legacy implementation
    /// for incremental migration.
    /// </summary>
    internal sealed class BaseLayerFsm : ILocomotionAnimationLayer
    {
        private const string BaseLayerName = "BaseLocomotion";

        public int LayerIndex => 0;
        public AnimancerLayer Layer { get; set; }

        private readonly StateMachine<BaseStateKey, BaseLayerFsmState> stateMachine;
        private readonly BaseIdleState idleState;
        private readonly BaseTurnInPlaceState turnInPlaceState;
        private readonly BaseIdleToMovingState idleToMovingState;
        private readonly BaseMovingState moveState;

        private StringAsset lastPlayedAlias;
        private AnimancerState currentState;
        private SLocomotionAnimationLayerSnapshot lastSnapshot;

        // Cached context for states.
        public LocomotionAnimationContext context;

        public string LayerName => BaseLayerName;

        public SLocomotionAnimationLayerSnapshot AnimationSnapshot => lastSnapshot;

        internal AnimancerStringProfile AliasProfile => context.Alias;
        internal SLocomotion Snapshot => context.Snapshot;
        internal LocomotionProfile LocomotionProfile => context.LocomotionProfile;

        public BaseLayerFsm(AnimancerLayer layer)
        {
            Layer = layer;

            idleState = new BaseIdleState(this);
            turnInPlaceState = new BaseTurnInPlaceState(this);
            moveState = new BaseMovingState(this);
            idleToMovingState = new BaseIdleToMovingState(this);

            stateMachine = new StateMachine<BaseStateKey, BaseLayerFsmState>();
            stateMachine.Dictionary[BaseStateKey.Idle] = idleState;
            stateMachine.Dictionary[BaseStateKey.TurnInPlace] = turnInPlaceState;
            stateMachine.Dictionary[BaseStateKey.IdleToMoving] = idleToMovingState;
            stateMachine.Dictionary[BaseStateKey.Moving] = moveState;
        }

        public void Update(in LocomotionAnimationContext context)
        {
            this.context = context;
            
            EnsureInitialized();

            // The FSM states are responsible for driving transitions.
            stateMachine.CurrentState?.Tick();

            float normalizedTime = currentState != null ? (float)currentState.NormalizedTime : 0f;
            bool isTurnAnimation = IsTurnAlias(lastPlayedAlias, AliasProfile);
            lastSnapshot = new SLocomotionAnimationLayerSnapshot(
                layerName: BaseLayerName,
                alias: lastPlayedAlias,
                normalizedTime: normalizedTime,
                isTurnAnimation: isTurnAnimation);
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
