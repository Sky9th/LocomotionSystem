using System;
using System.Collections.Generic;
using Game.Locomotion.Agent;

namespace Game.Locomotion.Input
{
    /// <summary>
    /// Central input module for LocomotionAgentV2.
    ///
    /// - Subscribes to global EventDispatcher once.
    /// - Buffers the latest IAction payloads per type.
    /// - Exposes a type-safe TryGetAction API for the agent.
    /// </summary>
    internal sealed class LocomotionInputModule
    {
        private readonly Agent.LocomotionAgent owner;
        private readonly Dictionary<Type, object> actionBuffer = new();

        private SPlayerMoveIAction lastMoveAction = SPlayerMoveIAction.None;
        private SPlayerLookIAction lastLookAction = SPlayerLookIAction.None;

        private EventDispatcher eventDispatcher;
        private bool isSubscribed;

        internal LocomotionInputModule(Agent.LocomotionAgent owner)
        {
            this.owner = owner;
        }

        internal void Subscribe()
        {
            if (isSubscribed || owner == null)
            {
                return;
            }

            if (!TryResolveDispatcher(out eventDispatcher))
            {
                return;
            }

            eventDispatcher.Subscribe<SPlayerMoveIAction>(OnMoveAction);
            eventDispatcher.Subscribe<SPlayerLookIAction>(OnLookAction);

            isSubscribed = true;
        }

        internal void Unsubscribe()
        {
            if (!isSubscribed || eventDispatcher == null)
            {
                return;
            }

            eventDispatcher.Unsubscribe<SPlayerMoveIAction>(OnMoveAction);
            eventDispatcher.Unsubscribe<SPlayerLookIAction>(OnLookAction);

            eventDispatcher = null;
            isSubscribed = false;
            actionBuffer.Clear();
        }

        private void OnMoveAction(SPlayerMoveIAction action, MetaStruct meta)
        {
            if (owner == null || !owner.isActiveAndEnabled)
            {
                return;
            }
            lastMoveAction = action;
            actionBuffer[typeof(SPlayerMoveIAction)] = action;
        }

        private void OnLookAction(SPlayerLookIAction action, MetaStruct meta)
        {
            if (owner == null || !owner.isActiveAndEnabled)
            {
                return;
            }
            lastLookAction = action;
            actionBuffer[typeof(SPlayerLookIAction)] = action;
        }

        internal void GetLatestInput(out SPlayerMoveIAction moveAction, out SPlayerLookIAction lookAction)
        {
            moveAction = lastMoveAction;
            lookAction = lastLookAction;
        }

        private static bool TryResolveDispatcher(out EventDispatcher dispatcher)
        {
            dispatcher = null;
            GameContext context = GameContext.Instance;
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
}
