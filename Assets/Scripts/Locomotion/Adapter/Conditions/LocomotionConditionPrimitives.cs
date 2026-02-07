using UnityEngine;
using Game.Locomotion.Adapter;

namespace Game.Locomotion.Adapter.Conditions
{
    internal sealed class SpeedGreaterThanCondition : ILocomotionCondition
    {
        private readonly float threshold;

        public SpeedGreaterThanCondition(float threshold)
        {
            this.threshold = threshold;
        }

        public bool Evaluate(LocomotionAnimancerAdapter adapter)
        {
            var snapshot = adapter.Agent.Snapshot;
            return snapshot.Speed > threshold;
        }
    }

    internal sealed class SpeedLessOrEqualCondition : ILocomotionCondition
    {
        private readonly float threshold;

        public SpeedLessOrEqualCondition(float threshold)
        {
            this.threshold = threshold;
        }

        public bool Evaluate(LocomotionAnimancerAdapter adapter)
        {
            var snapshot = adapter.Agent.Snapshot;
            return snapshot.Speed <= threshold;
        }
    }

    internal sealed class SpeedLessThanCondition : ILocomotionCondition
    {
        private readonly float threshold;

        public SpeedLessThanCondition(float threshold)
        {
            this.threshold = threshold;
        }

        public bool Evaluate(LocomotionAnimancerAdapter adapter)
        {
            var snapshot = adapter.Agent.Snapshot;
            return snapshot.Speed < threshold;
        }
    }

    internal sealed class IsTurningCondition : ILocomotionCondition
    {
        public bool Evaluate(LocomotionAnimancerAdapter adapter)
        {
            var snapshot = adapter.Agent.Snapshot;
            return snapshot.IsTurning;
        }
    }

    internal sealed class AndCondition : ILocomotionCondition
    {
        private readonly ILocomotionCondition first;
        private readonly ILocomotionCondition second;

        public AndCondition(ILocomotionCondition first, ILocomotionCondition second)
        {
            this.first = first;
            this.second = second;
        }

        public bool Evaluate(LocomotionAnimancerAdapter adapter)
        {
            return first.Evaluate(adapter) && second.Evaluate(adapter);
        }
    }

    internal sealed class NotCondition : ILocomotionCondition
    {
        private readonly ILocomotionCondition condition;

        public NotCondition(ILocomotionCondition condition)
        {
            this.condition = condition;
        }

        public bool Evaluate(LocomotionAnimancerAdapter adapter)
        {
            return !condition.Evaluate(adapter);
        }
    }
}
