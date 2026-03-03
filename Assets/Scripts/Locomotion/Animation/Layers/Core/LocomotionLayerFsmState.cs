namespace Game.Locomotion.Animation.Layers.Core
{
    internal abstract class LocomotionLayerFsmState<TOwner> : Animancer.FSM.State
    {
        protected readonly TOwner Owner;

        protected LocomotionLayerFsmState(TOwner owner)
        {
            Owner = owner;
        }

        public abstract void Tick();
    }
}
