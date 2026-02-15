using UnityEngine;

internal sealed class MoveActionHandler : ActionHandler<SMoveIAction>
{
    internal MoveActionHandler(LocomotionAgent owner) : base(owner)
    {
    }

    protected override void Execute(SMoveIAction action, MetaStruct meta)
    {
        Owner.BufferIAction(action);
    }
}
