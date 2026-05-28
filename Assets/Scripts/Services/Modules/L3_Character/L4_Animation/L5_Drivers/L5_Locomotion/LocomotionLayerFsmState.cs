using Animancer.FSM;

namespace Game.Character.Animation.Drivers
{
    internal abstract class LocomotionLayerFsmState<TOwner> : State
    {
        protected readonly TOwner Owner;

        protected LocomotionLayerFsmState(TOwner owner)
        {
            Owner = owner;
        }

        public abstract void Tick();
    }
}
