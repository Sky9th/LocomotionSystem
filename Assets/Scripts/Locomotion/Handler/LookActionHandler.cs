internal sealed class LookActionHandler : ActionHandler<SPlayerLookIAction>
{
    internal LookActionHandler(LocomotionAgent owner) : base(owner)
    {
    }

    protected override void Execute(SPlayerLookIAction action, MetaStruct meta)
    {
        Owner.ApplyLookAction(action);
    }
}
