using System;
using System.Collections.Generic;

namespace Game.Locomotion.Input
{
    /// <summary>
    /// Central input module for LocomotionAgentV2.
    ///
    /// - Subscribes to global EventDispatcher once.
    /// - For each received IAction payload, forwards it into the owning
    ///   LocomotionAgent's internal input buffer via TryPutAction.
    /// - Does not maintain its own IAction buffer.
    /// </summary>
    internal sealed class LocomotionInputModule
    {
        private readonly struct InputActionSubscription
        {
            public readonly Action<EventDispatcher> Subscribe;
            public readonly Action<EventDispatcher> Unsubscribe;

            public InputActionSubscription(Action<EventDispatcher> subscribe, Action<EventDispatcher> unsubscribe)
            {
                Subscribe = subscribe;
                Unsubscribe = unsubscribe;
            }
        }

        private readonly Game.Locomotion.Agent.LocomotionAgent owner;
        private readonly Dictionary<Type, InputActionSubscription> subscriptions = new();
        private EventDispatcher eventDispatcher;
        private bool isSubscribed;

        internal LocomotionInputModule(Game.Locomotion.Agent.LocomotionAgent owner)
        {
            this.owner = owner;

            // Register all IAction payload types this module cares about.
            RegisterAction<SMoveIAction>();
            RegisterAction<SLookIAction>();
            RegisterAction<SCrouchIAction>();
            RegisterAction<SProneIAction>();
            RegisterAction<SRunIAction>();
            RegisterAction<SStandIAction>();
            RegisterAction<SWalkIAction>();
            RegisterAction<SSprintIAction>();
            RegisterAction<SJumpIAction>();

            // Seed the agent's input buffer with sensible defaults so
            // downstream code can assume a value is always present.
            if (owner != null)
            {
                owner.TryPutAction(SMoveIAction.None);
                owner.TryPutAction(SLookIAction.None);
                owner.TryPutAction(SCrouchIAction.None);
                owner.TryPutAction(SProneIAction.None);
                owner.TryPutAction(SRunIAction.None);
                owner.TryPutAction(SStandIAction.None);
                owner.TryPutAction(SWalkIAction.None);
                owner.TryPutAction(SSprintIAction.None);
                owner.TryPutAction(SJumpIAction.None);
            }
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

            foreach (var entry in subscriptions.Values)
            {
                entry.Subscribe(eventDispatcher);
            }

            isSubscribed = true;
        }

        internal void Unsubscribe()
        {
            if (!isSubscribed || eventDispatcher == null)
            {
                return;
            }

            foreach (var entry in subscriptions.Values)
            {
                entry.Unsubscribe(eventDispatcher);
            }
            eventDispatcher = null;
            isSubscribed = false;
        }
        
        private void RegisterAction<TPayload>() where TPayload : struct
        {
            void Handler(TPayload payload, MetaStruct meta)
            {
                if (owner == null || !owner.isActiveAndEnabled)
                {
                    return;
                }

                owner.TryPutAction(payload);
            }

            subscriptions[typeof(TPayload)] = new InputActionSubscription(
                subscribe: dispatcher => dispatcher.Subscribe<TPayload>(Handler),
                unsubscribe: dispatcher => dispatcher.Unsubscribe<TPayload>(Handler));
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
