using System;
using System.Collections.Generic;

namespace Game.Locomotion.Input
{
    /// <summary>
    /// Central input module for LocomotionAgentV2.
    ///
    /// - Subscribes to global EventDispatcher once.
    /// - Aggregates received IAction payloads into a single
    ///   <see cref="SLocomotionInputActions"/> value.
    /// - Exposes the latest aggregated input to the owning Agent.
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

        private SMoveIAction moveAction;
        private SMoveIAction lastMoveAction;
        private SLookIAction lookAction;
        private SCrouchIAction crouchAction;
        private SProneIAction proneAction;
        private SWalkIAction walkAction;
        private SRunIAction runAction;
        private SSprintIAction sprintAction;
        private SJumpIAction jumpAction;
        private SStandIAction standAction;

        private SCameraContext cameraControl;
        private bool hasCameraControl;

        internal LocomotionInputModule(Game.Locomotion.Agent.LocomotionAgent owner)
        {
            this.owner = owner;

            Reset();

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

            RegisterAction<SCameraContext>();

        }

        internal void Reset()
        {
            moveAction = SMoveIAction.None;
            lastMoveAction = SMoveIAction.None;
            lookAction = SLookIAction.None;
            crouchAction = SCrouchIAction.None;
            proneAction = SProneIAction.None;
            walkAction = SWalkIAction.None;
            runAction = SRunIAction.None;
            sprintAction = SSprintIAction.None;
            jumpAction = SJumpIAction.None;
            standAction = SStandIAction.None;

            cameraControl = default;
            hasCameraControl = false;
        }

        internal void ReadActions(out SLocomotionInputActions actions)
        {
            actions = new SLocomotionInputActions(
                moveAction,
                lastMoveAction,
                lookAction,
                crouchAction,
                proneAction,
                walkAction,
                runAction,
                sprintAction,
                jumpAction,
                standAction);
        }

        internal void ReadCameraControl(out bool hasControl, out SCameraContext control)
        {
            hasControl = hasCameraControl;
            control = cameraControl;
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

                PutAction(payload);
            }

            subscriptions[typeof(TPayload)] = new InputActionSubscription(
                subscribe: dispatcher => dispatcher.Subscribe<TPayload>(Handler),
                unsubscribe: dispatcher => dispatcher.Unsubscribe<TPayload>(Handler));
        }

        private void PutAction<TPayload>(TPayload payload) where TPayload : struct
        {
            // Keep this explicit and type-safe to preserve intent.
            if (typeof(TPayload) == typeof(SMoveIAction))
            {
                lastMoveAction = moveAction;
                moveAction = (SMoveIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SLookIAction))
            {
                lookAction = (SLookIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SCrouchIAction))
            {
                crouchAction = (SCrouchIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SProneIAction))
            {
                proneAction = (SProneIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SWalkIAction))
            {
                walkAction = (SWalkIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SRunIAction))
            {
                runAction = (SRunIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SSprintIAction))
            {
                sprintAction = (SSprintIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SJumpIAction))
            {
                jumpAction = (SJumpIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SStandIAction))
            {
                standAction = (SStandIAction)(object)payload;
                return;
            }

            if (typeof(TPayload) == typeof(SCameraContext))
            {
                if (owner != null && owner.IsPlayer)
                {
                    cameraControl = (SCameraContext)(object)payload;
                    hasCameraControl = true;
                }
                return;
            }
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
