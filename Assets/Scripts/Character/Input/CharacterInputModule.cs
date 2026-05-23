using System;
using System.Collections.Generic;

namespace Game.Character.Input
{
    internal sealed class CharacterInputModule
    {
        private readonly struct Subscription
        {
            public readonly Action<EventDispatcherService> Subscribe;
            public readonly Action<EventDispatcherService> Unsubscribe;
            public Subscription(Action<EventDispatcherService> subscribe, Action<EventDispatcherService> unsubscribe)
            {
                Subscribe = subscribe;
                Unsubscribe = unsubscribe;
            }
        }

        private readonly Game.Character.Components.CharacterActor owner;

        private SIActionMove moveAction;
        private SIActionMove lastMoveAction;
        private SIActionLook lookAction;
        private SIActionCrouch crouchAction;
        private SIActionProne proneAction;
        private SIActionWalk walkAction;
        private SIActionRun runAction;
        private SIActionSprint sprintAction;
        private SIActionJump jumpAction;
        private SIActionStand standAction;

        private SCameraContext cameraControl;
        private bool hasCameraControl;

        private bool isSubscribed;
        private EventDispatcherService eventDispatcher;
        private readonly Dictionary<Type, Subscription> subscriptions = new();

        internal CharacterInputModule(Game.Character.Components.CharacterActor owner)
        {
            this.owner = owner;

            Register<SIActionMove>();
            Register<SIActionLook>();
            Register<SIActionCrouch>();
            Register<SIActionProne>();
            Register<SIActionRun>();
            Register<SIActionStand>();
            Register<SIActionWalk>();
            Register<SIActionSprint>();
            Register<SIActionJump>();
            Register<SCameraContext>();
        }

        internal void Reset()
        {
            moveAction = SIActionMove.None;
            lastMoveAction = SIActionMove.None;
            lookAction = SIActionLook.None;
            crouchAction = SIActionCrouch.None;
            proneAction = SIActionProne.None;
            walkAction = SIActionWalk.None;
            runAction = SIActionRun.None;
            sprintAction = SIActionSprint.None;
            jumpAction = SIActionJump.None;
            standAction = SIActionStand.None;
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
            if (typeof(TPayload) == typeof(SIActionMove))
            {
                lastMoveAction = moveAction;
                moveAction = (SIActionMove)(object)payload;
                return;
            }
            if (typeof(TPayload) == typeof(SIActionLook))
            { lookAction = (SIActionLook)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionCrouch))
            { crouchAction = (SIActionCrouch)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionProne))
            { proneAction = (SIActionProne)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionWalk))
            { walkAction = (SIActionWalk)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionRun))
            { runAction = (SIActionRun)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionSprint))
            { sprintAction = (SIActionSprint)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionJump))
            { jumpAction = (SIActionJump)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionStand))
            { standAction = (SIActionStand)(object)payload; return; }
            if (typeof(TPayload) == typeof(SCameraContext))
            {
                if (owner != null && owner.IsPlayer)
                {
                    cameraControl = (SCameraContext)(object)payload;
                    hasCameraControl = true;
                }
            }
        }

        private static bool TryResolveDispatcher(out EventDispatcherService dispatcher)
        {
            dispatcher = null;
            var context = GameContext.Instance;
            if (context == null) return false;
            if (!context.TryResolveService(out dispatcher)) return false;
            return true;
        }
    }
}
