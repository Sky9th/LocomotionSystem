using UnityEngine;

namespace Game.Locomotion.Adapter.Conditions
{
    internal static class LocomotionConditions
    {
        public static readonly ILocomotionCondition IsMoving =
            new SpeedGreaterThanCondition(Mathf.Epsilon);

        public static readonly ILocomotionCondition IsStopped =
            new SpeedLessOrEqualCondition(Mathf.Epsilon);

        public static readonly ILocomotionCondition IsTurning =
            new IsTurningCondition();

        public static readonly ILocomotionCondition NotTurning =
            new NotCondition(IsTurning);

        public static readonly ILocomotionCondition IsTurningInPlace =
            new AndCondition(IsTurning, new SpeedLessThanCondition(Mathf.Epsilon));
    }
}
