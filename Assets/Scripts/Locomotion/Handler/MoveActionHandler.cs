using UnityEngine;

internal sealed class MoveActionHandler : ActionHandler<SPlayerMoveIAction>
{
    internal MoveActionHandler(LocomotionAgent owner) : base(owner)
    {
    }

    protected override void Execute(SPlayerMoveIAction action, MetaStruct meta)
    {
        Owner.BufferIAction(action);
    }
}
