using Animancer.FSM;

namespace RedDust.Gameplay.Character.Animation.Drivers.Locomotion
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
