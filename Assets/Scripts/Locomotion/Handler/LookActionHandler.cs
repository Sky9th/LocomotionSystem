internal sealed class LookActionHandler : ActionHandler<SLookIAction>
{
    internal LookActionHandler(LocomotionAgent owner) : base(owner)
    {
    }

    protected override void Execute(SLookIAction action, MetaStruct meta)
    {
        Owner.BufferIAction(action);
    }
}
