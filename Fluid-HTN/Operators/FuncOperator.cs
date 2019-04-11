using System;

namespace FluidHTN.Operators
{
    public class FuncOperator<T> : IOperator where T : IContext
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

        public TaskStatus Update(IContext ctx)
        {
            if (ctx is T c)
                return _func?.Invoke(c) ?? TaskStatus.Failure;
            throw new Exception("Unexpected context type!");
        }

        public void Stop(IContext ctx)
        {
            if (ctx is T c)
                _funcStop?.Invoke(c);
            else
                throw new Exception("Unexpected context type!");
        }
    }
}