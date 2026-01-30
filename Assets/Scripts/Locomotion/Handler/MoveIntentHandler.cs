using UnityEngine;

internal sealed class MoveIntentHandler : IntentHandler<PlayerMoveIntentStruct>
{
	internal MoveIntentHandler(LocomotionAgent owner) : base(owner)
	{
	}

	protected override void Execute(PlayerMoveIntentStruct intent, MetaStruct meta)
	{
		Owner.BufferPlayerMoveIntent(intent);
	}
}
