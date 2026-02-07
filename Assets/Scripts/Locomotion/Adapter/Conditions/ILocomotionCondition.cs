using Game.Locomotion.Adapter;

namespace Game.Locomotion.Adapter.Conditions
{
    internal interface ILocomotionCondition
    {
        bool Evaluate(LocomotionAnimancerAdapter adapter);
    }
}
