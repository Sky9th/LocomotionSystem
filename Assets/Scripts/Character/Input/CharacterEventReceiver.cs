using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character.Input
{
    internal sealed class CharacterEventReceiver
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
        private SIActionPrimaryInteract primaryInteractAction;
        private SIActionSecondaryInteract secondaryInteractAction;

        // Camera
        private SCameraSnapshot cameraControl;
        private bool hasCameraControl;
        private Vector3 mouseGroundPosition;
        private bool hasMouseGround;

        private bool isSubscribed;
        private EventDispatcherService eventDispatcher;
        private readonly Dictionary<Type, Subscription> subscriptions = new();

        internal CharacterEventReceiver(Game.Character.Components.CharacterActor owner)
        {
            this.owner = owner;

            // TODO: WASD movement disabled — Phase 4 A* Pathfinding will drive movement via GridAgent
            // Register<SIActionMove>();
            Register<SIActionLook>();
            Register<SIActionCrouch>();
            Register<SIActionProne>();
            Register<SIActionRun>();
            Register<SIActionStand>();
            Register<SIActionWalk>();
            Register<SIActionSprint>();
            Register<SIActionJump>();
            Register<SIActionPrimaryInteract>();
            Register<SIActionSecondaryInteract>();
            RegisterCamera();
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
            primaryInteractAction = SIActionPrimaryInteract.None;
            secondaryInteractAction = SIActionSecondaryInteract.None;
            cameraControl = default;
            hasCameraControl = false;
            mouseGroundPosition = Vector3.zero;
            hasMouseGround = false;
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
            primaryInteractAction = primaryInteractAction.ClearFrameSignals();
            secondaryInteractAction = secondaryInteractAction.ClearFrameSignals();
        }

        internal bool ReadPrimaryInteract(out SIActionPrimaryInteract action)
        {
            action = primaryInteractAction;
            return primaryInteractAction.Button.IsRequested;
        }

        internal bool ReadSecondaryInteract(out SIActionSecondaryInteract action)
        {
            action = secondaryInteractAction;
            return secondaryInteractAction.Button.IsRequested;
        }

        internal bool ReadCameraControl(out SCameraSnapshot control)
        {
            control = cameraControl;
            return hasCameraControl;
        }

        internal bool ReadMouseGroundPosition(out Vector3 worldPosition)
        {
            worldPosition = mouseGroundPosition;
            return hasMouseGround;
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

        // ── Camera ──

        private void RegisterCamera()
        {
            subscriptions[typeof(SCameraSnapshot)] = new Subscription(
                d => d.Subscribe<SCameraSnapshot>(HandleCameraSnapshot),
                d => d.Unsubscribe<SCameraSnapshot>(HandleCameraSnapshot));
        }

        private void HandleCameraSnapshot(SCameraSnapshot snapshot, MetaStruct meta)
        {
            if (owner == null || !owner.isActiveAndEnabled) return;
            if (!owner.IsPlayer) return;
            cameraControl = snapshot;
            hasCameraControl = true;
            mouseGroundPosition = snapshot.MouseGroundPosition;
            hasMouseGround = snapshot.IsMouseGroundValid;
        }

        // ── Input Actions ──

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
            if (typeof(TPayload) == typeof(SIActionPrimaryInteract))
            { primaryInteractAction = (SIActionPrimaryInteract)(object)payload; LogInteract("PrimaryInteract", primaryInteractAction.Button); return; }
            if (typeof(TPayload) == typeof(SIActionSecondaryInteract))
            { secondaryInteractAction = (SIActionSecondaryInteract)(object)payload; LogInteract("SecondaryInteract", secondaryInteractAction.Button); return; }
        }

        private static void LogInteract(string name, SButtonInputState state)
        {
            if (state.IsRequested)
                Debug.Log($"[Character] {name} — Pressed");
            else if (state.IsReleased)
                Debug.Log($"[Character] {name} — Released");
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
