using System;
using System.Collections.Generic;
using RedDust.Core;
using RedDust.GameState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 输入服务。遍历 InputActionAsset 的所有 Action，
    /// 按动作名匹配 eventChannels 中的 GameEvent&lt;T&gt; SO，自动绑定 InputSystem → Raise(payload)。
    /// </summary>
    [DisallowMultipleComponent]
    public class InputService : ModuleChildMono
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private GameEvent[] eventChannels;

        private readonly List<Action> _teardown = new();

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire() { }

        private void OnEnable()
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("[InputService] No InputActionAsset assigned — OnEnable aborted.");
                return;
            }

            var channelByName = new Dictionary<string, GameEvent>();
            if (eventChannels != null && eventChannels.Length > 0)
                foreach (var ch in eventChannels)
                    if (ch != null && !channelByName.ContainsKey(ch.name))
                        channelByName[ch.name] = ch;

            if (channelByName.Count == 0)
            {
                Debug.LogError("[InputService] eventChannels is empty — nothing to bind. Drag input SOs into the array.");
                return;
            }

            foreach (var map in inputActionAsset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    if (!channelByName.TryGetValue(action.name, out var ch)) continue;

                    if (ch is GameEvent<SButtonInputPayload> buttonChannel)
                        BindButton(action, buttonChannel);
                    else if (ch is GameEvent<SVector2InputPayload> vector2Channel)
                        BindVector2(action, vector2Channel);
                    else if (ch is GameEvent<SFloatInputPayload> floatChannel)
                        BindFloat(action, floatChannel);
                }
            }

            // Enable all actions — InputSystem requires explicit Enable before callbacks fire.
            foreach (var map in inputActionAsset.actionMaps)
                map.Enable();
        }

        private void OnDisable()
        {
            if (inputActionAsset != null)
                foreach (var map in inputActionAsset.actionMaps)
                    map.Disable();

            foreach (var a in _teardown) a?.Invoke();
            _teardown.Clear();
        }

        private void OnDestroy() => OnDisable();

        // ── Binding helpers ──

        private void BindButton(InputAction action, GameEvent<SButtonInputPayload> channel)
        {
            Action<InputAction.CallbackContext> onPerformed = ctx =>
                channel.Raise(new SButtonInputPayload(true, true, false,
                    ctx.action.GetBindingIndexForControl(ctx.control)));
            Action<InputAction.CallbackContext> onCanceled = ctx =>
                channel.Raise(new SButtonInputPayload(false, false, true,
                    ctx.action.GetBindingIndexForControl(ctx.control)));
            action.performed += onPerformed;
            action.canceled += onCanceled;
            _teardown.Add(() =>
            {
                action.performed -= onPerformed;
                action.canceled -= onCanceled;
            });
        }

        private void BindVector2(InputAction action, GameEvent<SVector2InputPayload> channel)
        {
            Action<InputAction.CallbackContext> onPerformed = ctx =>
                channel.Raise(new SVector2InputPayload(ctx.ReadValue<Vector2>()));
            Action<InputAction.CallbackContext> onCanceled = _ =>
                channel.Raise(new SVector2InputPayload(Vector2.zero));
            action.performed += onPerformed;
            action.canceled += onCanceled;
            _teardown.Add(() =>
            {
                action.performed -= onPerformed;
                action.canceled -= onCanceled;
            });
        }

        private void BindFloat(InputAction action, GameEvent<SFloatInputPayload> channel)
        {
            Action<InputAction.CallbackContext> onPerformed = ctx =>
                channel.Raise(new SFloatInputPayload(ctx.ReadValue<float>()));
            Action<InputAction.CallbackContext> onCanceled = _ =>
                channel.Raise(new SFloatInputPayload(0f));
            action.performed += onPerformed;
            action.canceled += onCanceled;
            _teardown.Add(() =>
            {
                action.performed -= onPerformed;
                action.canceled -= onCanceled;
            });
        }

        // TODO: GameState 门控暂未实装
        public void ApplyGameState(EGameState state) { }
    }
}
