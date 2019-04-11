using System;

namespace FluidHTN.Effects
{
    public class ActionEffect<T> : IEffect where T : IContext
    {
        // ========================================================= FIELDS

        private readonly Action<T, EffectType> _action;

        // ========================================================= CONSTRUCTION

        public ActionEffect(string name, EffectType type, Action<T, EffectType> action)
        {
            Name = name;
            Type = type;
            _action = action;
        }

        // ========================================================= PROPERTIES

        public string Name { get; }
        public EffectType Type { get; }

        // ========================================================= FUNCTIONALITY

        public void Apply(IContext ctx)
        {
            if (ctx is T c)
                _action?.Invoke(c, Type);
            else
                throw new Exception("Unexpected context type!");
        }
    }
}