using System;

namespace RedDust.Stats
{
    public class StatModifier
    {
        public object Owner;
        public Action<StatInstance, ModifierContext> Apply;
    }
}
