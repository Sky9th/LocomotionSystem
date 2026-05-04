using Animancer;
using Animancer.FSM;
using Game.Character.Locomotion;

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
        internal LocomotionProfile LocoProfile { get; }
        internal SCharacterSnapshot Snapshot => snapshot;
        internal float DeltaTime => deltaTime;
        internal AnimancerLayer Layer { get; }

        internal BaseLayer(AnimancerLayer layer, LocomotionAliasProfile alias, LocomotionProfile locoProfile)
        {
            Layer = layer;
            Alias = alias;
            LocoProfile = locoProfile;
            fsm = new StateMachine<BaseStateKey, LocomotionLayerFsmState<BaseLayer>>();
            fsm.Dictionary[BaseStateKey.Idle] = new BaseIdleState(this);
            fsm.Dictionary[BaseStateKey.Moving] = new BaseMovingState(this);
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
