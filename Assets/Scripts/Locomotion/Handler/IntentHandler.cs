internal abstract class IntentHandler<TIntent>
{
    private EventDispatcher eventDispatcher;
    private bool isSubscribed;

    protected IntentHandler(LocomotionAgent owner)
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

        eventDispatcher.Subscribe<TIntent>(OnIntent);
        isSubscribed = true;
    }

    internal void Unsubscribe()
    {
        if (!isSubscribed || eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.Unsubscribe<TIntent>(OnIntent);
        eventDispatcher = null;
        isSubscribed = false;
    }

    private void OnIntent(TIntent intent, MetaStruct meta)
    {
        if (Owner == null || !Owner.IsRegistered)
        {
            return;
        }

        Execute(intent, meta);
    }

    protected abstract void Execute(TIntent intent, MetaStruct meta);

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
