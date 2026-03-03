using Game.Locomotion.Animation.Core;

namespace Game.Locomotion.Animation.Conditions
{
    internal static class CheckExtensions
    {
        public static bool Check<TCheck>(this in LocomotionAnimationContext context)
            where TCheck : struct, ICheck<LocomotionAnimationContext>
        {
            return default(TCheck).Evaluate(in context);
        }
    }
}
