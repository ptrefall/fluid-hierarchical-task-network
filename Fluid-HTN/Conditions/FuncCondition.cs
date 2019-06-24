using System;

namespace FluidHTN.Conditions
{
    public class FuncCondition<T> : ICondition where T : IContext
    {
        // ========================================================= FIELDS

        private readonly Func<T, bool> _func;

        // ========================================================= CONSTRUCTION

        public FuncCondition(string name, Func<T, bool> func)
        {
            Name = name;
            _func = func;
        }

        // ========================================================= PROPERTIES

        public string Name { get; }
        public int Depth { get; set; }

        // ========================================================= VALIDITY

        public bool IsValid(IContext ctx)
        {
            if (ctx is T c)
            {
                var result = _func?.Invoke(c) ?? false;
                if (ctx.LogDecomposition) ctx.Log(Name, $"FuncCondition.IsValid:{result}", Depth, this);
                return result;
            }

            throw new Exception("Unexpected context type!");
        }
    }
}
