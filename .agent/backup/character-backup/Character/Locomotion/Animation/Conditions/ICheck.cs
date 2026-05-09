namespace Game.Locomotion.Animation.Conditions
{
    internal interface ICheck<TContext>
    {
        bool Evaluate(in TContext context);
    }
}
