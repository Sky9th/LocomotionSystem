using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// 单个 Float 属性的运行时状态。由 EntityProperties 内部持有。
    /// 职责：可变 Current + Min/Max 钳制 + 消耗/恢复 Tick + Modifier 管理 + 事件广播。
    /// </summary>
    public class FloatState
    {
        public string Path { get; }
        public float Current { get; private set; }
        public float Min { get; }
        public float Max { get; }
        public bool IsConsumable { get; }
        public float ConsumeRate { get; }
        public float ConsumeInterval { get; }
        public bool IsRestorable { get; }
        public float RestoreRate { get; }
        public float RestoreInterval { get; }

        // 按类型 + 频率分桶
        private readonly List<FloatModifier> _rateMods = new();            // A，每帧执行
        private readonly List<FloatModifier> _deltaEveryFrame = new();     // B，每帧
        private readonly List<FloatModifier> _deltaPerSecond = new();      // B，每秒
        private readonly List<FloatModifier> _deltaPerMinute = new();      // B，每分钟
        private readonly List<DeltaCustom> _deltaCustom = new();           // B，自定义间隔
        private readonly List<CustomMod> _customEveryFrame = new();        // C，每帧
        private readonly List<CustomMod> _customScheduled = new();         // C，PerSecond/PerMinute/Custom

        // 共享计时器
        private float _secondTimer;
        private float _minuteTimer;
        private float _consumeTimer;
        private float _restoreTimer;

        private readonly RateContext _rateCtx = new();

        private struct DeltaCustom { public FloatModifier Mod; public float Timer; }
        private struct CustomMod { public FloatModifier Mod; public float Timer; public float Interval; }

        public event Action OnZero;
        public event Action<string, float, float> OnChanged;

        // === 构造 ===

        internal FloatState(
            string path, float min, float max, float initialValue,
            bool isConsumable, float consumeRate, float consumeInterval,
            bool isRestorable, float restoreRate, float restoreInterval)
        {
            Path = path; Min = min; Max = max;
            Current = Mathf.Clamp(initialValue, Min, Max);
            IsConsumable = isConsumable && consumeRate > 0f; ConsumeRate = consumeRate; ConsumeInterval = consumeInterval;
            IsRestorable = isRestorable && restoreRate > 0f; RestoreRate = restoreRate; RestoreInterval = restoreInterval;
        }

        // === Modifier 管理 ===

        public void AddModifier(FloatModifier m)
        {
            if (m.CustomTick != null) { AddCustom(m); return; } // 自定义接管，不走 rate/delta
            if (m.OnApplyRate != null) _rateMods.Add(m);
            if (m.Delta != 0f) AddDelta(m);
        }

        private void AddDelta(FloatModifier m)
        {
            switch (m.Frequency)
            {
                case ModifierFrequency.PerFrame: _deltaEveryFrame.Add(m); break;
                case ModifierFrequency.PerSecond: _deltaPerSecond.Add(m); break;
                case ModifierFrequency.PerMinute: _deltaPerMinute.Add(m); break;
                default: _deltaCustom.Add(new DeltaCustom { Mod = m }); break;
            }
        }

        private void AddCustom(FloatModifier m)
        {
            float interval = m.Frequency switch
            {
                ModifierFrequency.PerSecond => 1f,
                ModifierFrequency.PerMinute => 60f,
                ModifierFrequency.Custom => m.CustomInterval,
                _ => 0f,
            };
            if (interval <= 0f) _customEveryFrame.Add(new CustomMod { Mod = m });
            else _customScheduled.Add(new CustomMod { Mod = m, Interval = interval });
        }

        public void RemoveModifiers(object owner)
        {
            _rateMods.RemoveAll(m => m.Owner == owner);
            _deltaEveryFrame.RemoveAll(m => m.Owner == owner);
            _deltaPerSecond.RemoveAll(m => m.Owner == owner);
            _deltaPerMinute.RemoveAll(m => m.Owner == owner);
            _deltaCustom.RemoveAll(d => d.Mod.Owner == owner);
            _customEveryFrame.RemoveAll(c => c.Mod.Owner == owner);
            _customScheduled.RemoveAll(c => c.Mod.Owner == owner);
        }

        private bool HasScheduledFreq(ModifierFrequency f) { foreach (var c in _customScheduled) if (c.Mod.Frequency == f) return true; return false; }

        public int ModifierCount => _rateMods.Count + _deltaEveryFrame.Count + _deltaPerSecond.Count
            + _deltaPerMinute.Count + _deltaCustom.Count + _customEveryFrame.Count + _customScheduled.Count;

        // === Modify ===

        public void Modify(float delta)
        {
            float prev = Current;
            Current = Mathf.Clamp(Current + delta, Min, Max);
            if (Mathf.Abs(Current - prev) > 0.001f) OnChanged?.Invoke(Path, prev, Current);
            if (prev > Min && Current <= Min) OnZero?.Invoke();
        }

        public void SetCurrent(float value)
        {
            float prev = Current;
            Current = Mathf.Clamp(value, Min, Max);
            if (Mathf.Abs(Current - prev) > 0.001f) OnChanged?.Invoke(Path, prev, Current);
            if (prev > Min && Current <= Min) OnZero?.Invoke();
        }

        public void SetCurrentSilent(float value) => Current = Mathf.Clamp(value, Min, Max);

        // === Tick ===

        public void Tick(float dt)
        {
            bool hasAnyMod = _rateMods.Count > 0 || _deltaEveryFrame.Count > 0 || _deltaPerSecond.Count > 0
                || _deltaPerMinute.Count > 0 || _deltaCustom.Count > 0
                || _customEveryFrame.Count > 0 || _customScheduled.Count > 0;
            if (!hasAnyMod && !IsConsumable && !IsRestorable) return;

            // 只给有 modifier 的频率计时
            bool needSecond = _deltaPerSecond.Count > 0 || HasScheduledFreq(ModifierFrequency.PerSecond);
            bool needMinute = _deltaPerMinute.Count > 0 || HasScheduledFreq(ModifierFrequency.PerMinute);
            bool fireSecond = false, fireMinute = false;
            if (needSecond) { _secondTimer += dt; if (_secondTimer >= 1f) { _secondTimer %= 1f; fireSecond = true; } }
            if (needMinute) { _minuteTimer += dt; if (_minuteTimer >= 60f) { _minuteTimer %= 60f; fireMinute = true; } }

            // 1. Custom
            foreach (var cm in _customEveryFrame) cm.Mod.CustomTick(this, dt);
            for (int i = _customScheduled.Count - 1; i >= 0; i--)
            {
                var cm = _customScheduled[i];
                bool fire = (cm.Mod.Frequency == ModifierFrequency.PerSecond && fireSecond)
                         || (cm.Mod.Frequency == ModifierFrequency.PerMinute && fireMinute)
                         || (cm.Mod.Frequency == ModifierFrequency.Custom && (cm.Timer += dt) >= cm.Interval);
                if (fire) { if (cm.Mod.Frequency == ModifierFrequency.Custom) cm.Timer %= cm.Interval; cm.Mod.CustomTick(this, cm.Interval); }
                _customScheduled[i] = cm;
            }

            // 2. Rate → consume/restore
            _rateCtx.Addend = 0f; _rateCtx.Multiplier = 1f;
            foreach (var m in _rateMods) m.OnApplyRate?.Invoke(_rateCtx);
            if (IsConsumable) TickConsume(dt);
            if (IsRestorable) TickRestore(dt);

            // 3. Delta 每帧
            foreach (var m in _deltaEveryFrame)
                if (m.Condition?.Invoke() ?? true) Modify(m.Delta * dt);

            // 4. Delta 每秒
            if (fireSecond)
                foreach (var m in _deltaPerSecond)
                    if (m.Condition?.Invoke() ?? true) Modify(m.Delta);

            // 5. Delta 每分钟
            if (fireMinute)
                foreach (var m in _deltaPerMinute)
                    if (m.Condition?.Invoke() ?? true) Modify(m.Delta);

            // 6. Delta 自定义间隔
            for (int i = _deltaCustom.Count - 1; i >= 0; i--)
            {
                var dc = _deltaCustom[i]; dc.Timer += dt;
                if (dc.Timer >= dc.Mod.CustomInterval) { dc.Timer %= dc.Mod.CustomInterval; if (dc.Mod.Condition?.Invoke() ?? true) Modify(dc.Mod.Delta); }
                _deltaCustom[i] = dc;
            }
        }

        private void TickConsume(float dt)
        {
            if (ConsumeInterval > 0f) { _consumeTimer += dt; if (_consumeTimer < ConsumeInterval) return; int n = (int)(_consumeTimer / ConsumeInterval); _consumeTimer %= ConsumeInterval; float d = (-ConsumeRate + _rateCtx.Addend) * _rateCtx.Multiplier * n; if (d != 0f) Modify(d); }
            else { float d = (-ConsumeRate + _rateCtx.Addend) * _rateCtx.Multiplier * dt; if (d != 0f) Modify(d); }
        }

        private void TickRestore(float dt)
        {
            if (RestoreInterval > 0f) { _restoreTimer += dt; if (_restoreTimer < RestoreInterval) return; int n = (int)(_restoreTimer / RestoreInterval); _restoreTimer %= RestoreInterval; float d = (RestoreRate + _rateCtx.Addend) * _rateCtx.Multiplier * n; if (d != 0f) Modify(d); }
            else { float d = (RestoreRate + _rateCtx.Addend) * _rateCtx.Multiplier * dt; if (d != 0f) Modify(d); }
        }
    }
}
