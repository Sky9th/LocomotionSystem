using System;

namespace Game.Stats
{
    public class StatModifier
    {
        public object Owner;
        public Action<StatInstance, ModifierContext> Apply;
    }
}
