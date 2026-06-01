using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Character.Director
{
    /// <summary>
    /// 玩家输入聚合器。纯类，通过 EventChannels 订阅事件，缓存帧状态。
    /// 由 CharacterActor 驱动 BindEvents / UnbindEvents。
    /// </summary>
    internal sealed class PlayerInput : IEventListener
    {
        private readonly EventChannels channels;

        // ── 帧状态 ──
        internal bool SecondaryRequested { get; set; }
        internal bool SprintRequested { get; set; }
        internal bool CrouchRequested { get; set; }
        internal bool ProneRequested { get; set; }
        internal bool StandRequested { get; set; }

        // ── TEMP ──
        private EventDispatcherService dispatcher;
        private Vector3 mouseGroundPosition;
        private bool hasMouseGround;
        internal Vector3 MouseGroundPosition => mouseGroundPosition;
        internal bool HasMouseGround => hasMouseGround;

        internal PlayerInput(EventChannels channels)
        {
            this.channels = channels;
        }

        public void BindEvents()
        {
            channels.Get<SecondaryInteractEvent>()?.Register(OnSecondary);
            channels.Get<SprintInputEvent>()?.Register(OnSprint);
            channels.Get<CrouchInputEvent>()?.Register(OnCrouch);
            channels.Get<ProneInputEvent>()?.Register(OnProne);
            channels.Get<StandInputEvent>()?.Register(OnStand);

            // TEMP
            if (GameContext.Instance != null &&
                GameContext.Instance.TryResolveService(out dispatcher))
            {
                dispatcher.Subscribe<SCameraSnapshot>(OnCameraSnapshot);
            }
        }

        public void UnbindEvents()
        {
            channels.Get<SecondaryInteractEvent>()?.Unregister(OnSecondary);
            channels.Get<SprintInputEvent>()?.Unregister(OnSprint);
            channels.Get<CrouchInputEvent>()?.Unregister(OnCrouch);
            channels.Get<ProneInputEvent>()?.Unregister(OnProne);
            channels.Get<StandInputEvent>()?.Unregister(OnStand);

            dispatcher?.Unsubscribe<SCameraSnapshot>(OnCameraSnapshot);
            dispatcher = null;
        }

        // ── Handlers ──

        private void OnSecondary(bool p) { if (p) SecondaryRequested = true; }
        private void OnSprint(bool p) { if (p) SprintRequested = true; }
        private void OnCrouch(bool p) { if (p) CrouchRequested = true; }
        private void OnProne(bool p) { if (p) ProneRequested = true; }
        private void OnStand(bool p) { if (p) StandRequested = true; }

        // TEMP
        private void OnCameraSnapshot(SCameraSnapshot snapshot, MetaStruct _)
        {
            mouseGroundPosition = snapshot.MouseGroundPosition;
            hasMouseGround = snapshot.IsMouseGroundValid;
        }

        internal void ClearFrameSignals()
        {
            SecondaryRequested = SprintRequested = CrouchRequested =
                ProneRequested = StandRequested = false;
        }
    }
}
