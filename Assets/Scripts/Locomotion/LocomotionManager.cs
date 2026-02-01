using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局 Locomotion 注册器。负责协调各角色的 LocomotionAgent，并将关键快照同步到 GameContext。
/// </summary>
[DisallowMultipleComponent]
public class LocomotionManager : BaseService
{
    [SerializeField] private bool logRegistrations;
    [SerializeField] private LocomotionAgent defaultPlayerAgent;

    private readonly HashSet<LocomotionAgent> activeAgents = new();
    private readonly Dictionary<LocomotionAgent, SPlayerLocomotion> snapshotCache = new();
    [SerializeField] private List<LocomotionAgent> inspectorAgents = new();
    private LocomotionAgent playerAgent;

    public IReadOnlyCollection<LocomotionAgent> ActiveAgents => activeAgents;
    public LocomotionAgent PlayerAgent => playerAgent;

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);

        if (defaultPlayerAgent != null)
        {
            defaultPlayerAgent.TryRegisterWithManager();
        }

        return true;
    }

    internal bool RegisterComponent(LocomotionAgent agent)
    {
        if (!IsRegistered || agent == null)
        {
            return false;
        }

        if (activeAgents.Add(agent))
        {
            if (agent.IsPlayer)
            {
                playerAgent = agent;
            }

            RefreshInspectorList();

            if (logRegistrations)
            {
                Debug.Log($"Locomotion agent registered: {agent.name}", this);
            }
        }

        return true;
    }

    internal void UnregisterComponent(LocomotionAgent agent)
    {
        if (agent == null)
        {
            return;
        }

        activeAgents.Remove(agent);
        snapshotCache.Remove(agent);
        RefreshInspectorList();

        if (playerAgent == agent)
        {
            playerAgent = null;
        }
    }

    internal void PublishSnapshot(LocomotionAgent agent, SPlayerLocomotion snapshot)
    {
        if (!IsRegistered || agent == null)
        {
            return;
        }

        snapshotCache[agent] = snapshot;

        if (agent == playerAgent && GameContext != null)
        {
            GameContext.UpdateSnapshot(snapshot);
        }
    }

    public bool TryGetSnapshot(LocomotionAgent agent, out SPlayerLocomotion snapshot)
    {
        if (agent != null && snapshotCache.TryGetValue(agent, out snapshot))
        {
            return true;
        }

        snapshot = SPlayerLocomotion.Default;
        return false;
    }

    public bool TryGetPlayerSnapshot(out SPlayerLocomotion snapshot)
    {
        if (playerAgent != null)
        {
            return TryGetSnapshot(playerAgent, out snapshot);
        }

        snapshot = SPlayerLocomotion.Default;
        return false;
    }

    private void RefreshInspectorList()
    {
        inspectorAgents.Clear();
        inspectorAgents.AddRange(activeAgents);
    }
}
