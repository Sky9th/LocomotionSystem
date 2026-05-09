namespace Game.Locomotion.Animation.Conditions
{
    internal readonly struct OrCondition<TContext, TLeft, TRight> : ICheck<TContext>
        where TLeft : struct, ICheck<TContext>
        where TRight : struct, ICheck<TContext>
    {
        public bool Evaluate(in TContext context)
            => default(TLeft).Evaluate(in context) || default(TRight).Evaluate(in context);
    }
}
