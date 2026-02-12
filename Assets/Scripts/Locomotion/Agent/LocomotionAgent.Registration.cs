using UnityEngine;

/// <summary>
/// Registration, enable state, and handler wiring for LocomotionAgent.
/// Split out to keep the main agent file lean.
/// </summary>
public partial class LocomotionAgent : MonoBehaviour
{
    public bool TryRegisterWithManager()
    {
        if (isRegistered)
        {
            return true;
        }

        if (manager == null)
        {
            manager = FindManagerInScene();
        }

        if (manager == null || !manager.IsRegistered)
        {
            return false;
        }

        if (manager.RegisterComponent(this))
        {
            isRegistered = true;
            RegisterActionHandlers();
            return true;
        }

        return false;
    }

    private void RegisterActionHandlers()
    {
        if (subscribePlayerMoveAction)
        {
            moveActionHandler ??= new MoveActionHandler(this);
            moveActionHandler.Subscribe();
        }

        if (subscribePlayerLookAction)
        {
            lookActionHandler ??= new LookActionHandler(this);
            lookActionHandler.Subscribe();
        }
    }

    private void UnregisterActionHandlers()
    {
        moveActionHandler?.Unsubscribe();
        lookActionHandler?.Unsubscribe();
    }

    private LocomotionManager FindManagerInScene()
    {
        if (GameContext.Instance != null && GameContext.Instance.TryResolveService(out LocomotionManager resolved))
        {
            return resolved;
        }

        return FindObjectOfType<LocomotionManager>();
    }
}
