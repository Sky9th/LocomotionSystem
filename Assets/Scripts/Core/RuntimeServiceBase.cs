using UnityEngine;

/// <summary>
/// Shared base class for all runtime managers/services that the GameManager bootstraps.
/// Provides a deterministic Register(GameContext) entry point so each system can
/// bind into the global context without duplicating guard logic.
/// </summary>
public abstract class RuntimeServiceBase : MonoBehaviour
{
    public bool IsRegistered { get; private set; }
    protected GameContext GameContext { get; private set; }

    /// <summary>
    /// Called by GameManager to hook this service into the GameContext.
    /// Derived classes should place their initialization logic inside OnRegister.
    /// </summary>
    public void Register(GameContext context)
    {
        if (context == null)
        {
            Debug.LogError($"{name} cannot register without a valid GameContext reference.", this);
            return;
        }

        if (IsRegistered)
        {
            return;
        }

        GameContext = context;
        if (OnRegister(context))
        {
            IsRegistered = true;
        }
        else
        {
            GameContext = null;
        }
    }

    protected abstract bool OnRegister(GameContext context);
}
