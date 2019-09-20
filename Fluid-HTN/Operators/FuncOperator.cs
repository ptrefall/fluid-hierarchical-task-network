using System;

namespace FluidHTN.Operators
{
    public class FuncOperator<T, TWorldStateEntry> : IOperator<TWorldStateEntry> where T : IContext<TWorldStateEntry>
    {
        // ========================================================= FIELDS

        private readonly Func<T, TaskStatus> _func;
        private readonly Action<T> _funcStop;

        // ========================================================= CONSTRUCTION

        public FuncOperator(Func<T, TaskStatus> func, Action<T> funcStop = null)
        {
            _func = func;
            _funcStop = funcStop;
        }

        // ========================================================= FUNCTIONALITY

        public TaskStatus Update(IContext<TWorldStateEntry> ctx)
        {
            if (ctx is T c)
                return _func?.Invoke(c) ?? TaskStatus.Failure;
            throw new Exception("Unexpected context type!");
        }

        public void Stop(IContext<TWorldStateEntry> ctx)
        {
            if (ctx is T c)
                _funcStop?.Invoke(c);
            else
                throw new Exception("Unexpected context type!");
        }
    }
}
