using UnityEngine;

/// <summary>
/// Maintains high-level player wiring, including syncing the Player root with the animated Model child.
/// </summary>
[DisallowMultipleComponent]
public class PlayerManager : BaseService
{

    [SerializeField] private GameObject PlayerPrefab;

    [SerializeField] private GameObject PlayerStartAnchor;

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

        if (PlayerStartAnchor == null)
        {
            PlayerStartAnchor = GameObject.Find("PlayerStart");
        }

        GameObject playerInstance = Instantiate(PlayerPrefab);
        playerInstance.name = PlayerPrefab.name;
        if (PlayerStartAnchor != null)
        {
            playerInstance.transform.SetPositionAndRotation(PlayerStartAnchor.transform.position, PlayerStartAnchor.transform.rotation);
        }

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
            Dispatcher.Publish(new SPlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));
        }
    }
}
