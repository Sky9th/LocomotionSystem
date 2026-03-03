namespace Game.Locomotion.Animation.Conditions
{
    internal readonly struct NotCondition<TContext, TCheck> : ICheck<TContext>
        where TCheck : struct, ICheck<TContext>
    {
        public bool Evaluate(in TContext context)
            => !default(TCheck).Evaluate(in context);
    }
}
