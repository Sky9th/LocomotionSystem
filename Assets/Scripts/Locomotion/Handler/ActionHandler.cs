internal abstract class ActionHandler<TAction>
{
    private EventDispatcher eventDispatcher;
    private bool isSubscribed;

    protected ActionHandler(LocomotionAgent owner)
    {
        Owner = owner;
    }

    protected LocomotionAgent Owner { get; }

    internal void Subscribe()
    {
        if (isSubscribed || Owner == null)
        {
            return;
        }

        if (!TryResolveDispatcher(out eventDispatcher))
        {
            return;
        }

        eventDispatcher.Subscribe<TAction>(OnAction);
        isSubscribed = true;
    }

    internal void Unsubscribe()
    {
        if (!isSubscribed || eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.Unsubscribe<TAction>(OnAction);
        eventDispatcher = null;
        isSubscribed = false;
    }

    private void OnAction(TAction action, MetaStruct meta)
    {
        if (Owner == null || !Owner.IsRegistered)
        {
            return;
        }

        Execute(action, meta);
    }

    protected abstract void Execute(TAction action, MetaStruct meta);

    private static bool TryResolveDispatcher(out EventDispatcher dispatcher)
    {
        dispatcher = null;
        var context = GameContext.Instance;
        if (context == null)
        {
            return false;
        }

        if (!context.TryResolveService(out EventDispatcher resolved))
        {
            return false;
        }

        dispatcher = resolved;
        return true;
    }
}
