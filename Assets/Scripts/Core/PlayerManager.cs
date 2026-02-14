using UnityEngine;

/// <summary>
/// Maintains high-level player wiring, including syncing the Player root with the animated Model child.
/// </summary>
[DisallowMultipleComponent]
public class PlayerManager : BaseService
{

    [SerializeField] private GameObject PlayerPrefab;

    protected override void OnServicesReady()
    {
        CreatePlayer();
    }

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);
        return true;
    }

    private void CreatePlayer()
    {
        if (PlayerPrefab == null)
        {
            Debug.LogError("PlayerPrefab reference is missing in PlayerManager.", this);
            return;
        }

        GameObject playerInstance = Instantiate(PlayerPrefab);
        playerInstance.name = PlayerPrefab.name;

        // Broadcast a spawn snapshot so other systems can react
        // (camera follow, UI hooks, etc.). Also push it into the
        // GameContext snapshot registry for easy lookup.
        var playerSnapshot = SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true);

        if (GameContext != null)
        {
            GameContext.UpdateSnapshot(playerSnapshot);
        }

        if (Dispatcher != null)
        {
            Dispatcher.Publish(new PlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));
        }
    }
}
