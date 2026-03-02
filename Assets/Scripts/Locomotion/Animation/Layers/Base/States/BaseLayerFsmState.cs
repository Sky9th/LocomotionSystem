namespace Game.Locomotion.Animation.Layers.Base
{
    internal abstract class BaseLayerFsmState : Animancer.FSM.State
    {
        protected readonly BaseLayerFsm Owner;

        protected BaseLayerFsmState(BaseLayerFsm owner)
        {
            Owner = owner;
        }

        public abstract void Tick();
    }
}
