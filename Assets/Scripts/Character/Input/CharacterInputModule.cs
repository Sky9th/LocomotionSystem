using System;
using System.Collections.Generic;

namespace Game.Character.Input
{
    internal sealed class CharacterInputModule
    {
        private readonly struct Subscription
        {
            public readonly Action<EventDispatcher> Subscribe;
            public readonly Action<EventDispatcher> Unsubscribe;
            public Subscription(Action<EventDispatcher> subscribe, Action<EventDispatcher> unsubscribe)
            {
                Subscribe = subscribe;
                Unsubscribe = unsubscribe;
            }
        }

        private readonly Game.Character.Components.CharacterActor owner;

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

        private bool isSubscribed;
        private EventDispatcher eventDispatcher;
        private readonly Dictionary<Type, Subscription> subscriptions = new();

        internal CharacterInputModule(Game.Character.Components.CharacterActor owner)
        {
            this.owner = owner;

            Register<SMoveIAction>();
            Register<SLookIAction>();
            Register<SCrouchIAction>();
            Register<SProneIAction>();
            Register<SRunIAction>();
            Register<SStandIAction>();
            Register<SWalkIAction>();
            Register<SSprintIAction>();
            Register<SJumpIAction>();
            Register<SCameraContext>();
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

        internal void ReadActions(out SCharacterInputActions actions)
        {
            actions = new SCharacterInputActions(
                moveAction, lastMoveAction,
                lookAction,
                crouchAction, proneAction,
                walkAction, runAction,
                sprintAction, jumpAction,
                standAction);

            crouchAction = crouchAction.ClearFrameSignals();
            proneAction = proneAction.ClearFrameSignals();
            walkAction = walkAction.ClearFrameSignals();
            runAction = runAction.ClearFrameSignals();
            sprintAction = sprintAction.ClearFrameSignals();
            jumpAction = jumpAction.ClearFrameSignals();
            standAction = standAction.ClearFrameSignals();
        }

        internal bool ReadCameraControl(out SCameraContext control)
        {
            control = cameraControl;
            return hasCameraControl;
        }

        internal void Subscribe()
        {
            if (isSubscribed || owner == null) return;
            if (!TryResolveDispatcher(out eventDispatcher)) return;

            foreach (var s in subscriptions.Values)
                s.Subscribe(eventDispatcher);

            isSubscribed = true;
        }

        internal void Unsubscribe()
        {
            if (!isSubscribed || eventDispatcher == null) return;

            foreach (var s in subscriptions.Values)
                s.Unsubscribe(eventDispatcher);
            eventDispatcher = null;
            isSubscribed = false;
        }

        private void Register<TPayload>() where TPayload : struct
        {
            void Handler(TPayload payload, MetaStruct meta)
            {
                if (owner == null || !owner.isActiveAndEnabled) return;
                PutAction(payload);
            }

            subscriptions[typeof(TPayload)] = new Subscription(
                d => d.Subscribe<TPayload>(Handler),
                d => d.Unsubscribe<TPayload>(Handler));
        }

        private void PutAction<TPayload>(TPayload payload) where TPayload : struct
        {
            if (typeof(TPayload) == typeof(SMoveIAction))
            {
                lastMoveAction = moveAction;
                moveAction = (SMoveIAction)(object)payload;
                return;
            }
            if (typeof(TPayload) == typeof(SLookIAction))
            { lookAction = (SLookIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SCrouchIAction))
            { crouchAction = (SCrouchIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SProneIAction))
            { proneAction = (SProneIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SWalkIAction))
            { walkAction = (SWalkIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SRunIAction))
            { runAction = (SRunIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SSprintIAction))
            { sprintAction = (SSprintIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SJumpIAction))
            { jumpAction = (SJumpIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SStandIAction))
            { standAction = (SStandIAction)(object)payload; return; }
            if (typeof(TPayload) == typeof(SCameraContext))
            {
                if (owner != null && owner.IsPlayer)
                {
                    cameraControl = (SCameraContext)(object)payload;
                    hasCameraControl = true;
                }
            }
        }

        private static bool TryResolveDispatcher(out EventDispatcher dispatcher)
        {
            dispatcher = null;
            var context = GameContext.Instance;
            if (context == null) return false;
            if (!context.TryResolveService(out dispatcher)) return false;
            return true;
        }
    }
}
