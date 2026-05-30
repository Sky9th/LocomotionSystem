using System;
using System.Collections.Generic;
using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Character.Director
{
    /// <summary>
    /// 纯通道 — 订阅 Dispatcher、缓存本帧玩家输入事件。不包含任何翻译逻辑。
    /// </summary>
    internal sealed class PlayerInputReceiver
    {
        private readonly CharacterActor owner;

        // ── 帧缓存 ──
        internal SIActionWalk WalkAction;
        internal SIActionRun RunAction;
        internal SIActionSprint SprintAction;
        internal SIActionCrouch CrouchAction;
        internal SIActionProne ProneAction;
        internal SIActionStand StandAction;
        internal SIActionMove MoveAction;

        internal Vector3 MouseGroundPosition;
        internal bool HasMouseGround;

        // ── 订阅 ──
        private EventDispatcherService dispatcher;
        private bool isSubscribed;
        private readonly Dictionary<Type, (Action<EventDispatcherService> sub, Action<EventDispatcherService> unsub)> subscriptions = new();

        internal PlayerInputReceiver(CharacterActor owner)
        {
            this.owner = owner;
            RegisterEvents();
        }

        internal void Subscribe()
        {
            if (isSubscribed || owner == null) return;
            if (!TryResolveDispatcher(out dispatcher)) return;

            foreach (var (sub, _) in subscriptions.Values)
                sub(dispatcher);

            isSubscribed = true;
        }

        internal void Unsubscribe()
        {
            if (!isSubscribed || dispatcher == null) return;

            foreach (var (_, unsub) in subscriptions.Values)
                unsub(dispatcher);

            dispatcher = null;
            isSubscribed = false;
        }

        internal void Reset()
        {
            WalkAction = SIActionWalk.None;
            RunAction = SIActionRun.None;
            SprintAction = SIActionSprint.None;
            CrouchAction = SIActionCrouch.None;
            ProneAction = SIActionProne.None;
            StandAction = SIActionStand.None;
            MoveAction = SIActionMove.None;
            MouseGroundPosition = Vector3.zero;
            HasMouseGround = false;
        }

        // ── Event Registration ──

        private void RegisterEvents()
        {
            Register<SCameraSnapshot>(HandleCameraSnapshot);
            Register<SIActionWalk>(PutAction);
            Register<SIActionRun>(PutAction);
            Register<SIActionSprint>(PutAction);
            Register<SIActionCrouch>(PutAction);
            Register<SIActionProne>(PutAction);
            Register<SIActionStand>(PutAction);
            // TODO Phase 4
            // Register<SIActionMove>(PutAction);
        }

        private void Register<TPayload>(Action<TPayload, MetaStruct> handler) where TPayload : struct
        {
            subscriptions[typeof(TPayload)] = (
                d => d.Subscribe(handler),
                d => d.Unsubscribe(handler));
        }

        private void HandleCameraSnapshot(SCameraSnapshot snapshot, MetaStruct _)
        {
            if (owner == null || !owner.isActiveAndEnabled) return;
            if (!owner.IsPlayer) return;

            MouseGroundPosition = snapshot.MouseGroundPosition;
            HasMouseGround = snapshot.IsMouseGroundValid;
        }

        private void PutAction<TPayload>(TPayload payload, MetaStruct _) where TPayload : struct
        {
            if (owner == null || !owner.isActiveAndEnabled) return;

            if (typeof(TPayload) == typeof(SIActionWalk))
            { WalkAction = (SIActionWalk)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionRun))
            { RunAction = (SIActionRun)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionSprint))
            { SprintAction = (SIActionSprint)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionCrouch))
            { CrouchAction = (SIActionCrouch)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionProne))
            { ProneAction = (SIActionProne)(object)payload; return; }
            if (typeof(TPayload) == typeof(SIActionStand))
            { StandAction = (SIActionStand)(object)payload; return; }
        }

        private static bool TryResolveDispatcher(out EventDispatcherService dispatcher)
        {
            dispatcher = null;
            var context = GameContext.Instance;
            if (context == null) return false;
            return context.TryResolveService(out dispatcher);
        }
    }
}
