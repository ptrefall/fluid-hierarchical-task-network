using System;

namespace FluidHTN.Operators
{
    public class FuncOperator<T> : IOperator where T : IContext
    {
        // ========================================================= FIELDS

        private readonly Func<T, TaskStatus> _func;
        private readonly Func<T, TaskStatus> _start;
        private readonly Action<T> _funcStop;
        private readonly Action<T> _funcAborted;

        // ========================================================= CONSTRUCTION

        public FuncOperator(Func<T, TaskStatus> func, Func<T, TaskStatus> start = null, Action<T> funcStop = null, Action<T> funcAborted = null)
        {
            _func = func;
            _start = start;
            _funcStop = funcStop;
            _funcAborted = funcAborted;
        }

        // ========================================================= FUNCTIONALITY

        public TaskStatus Start(IContext ctx)
        {
            if (ctx is T c)
            {
                if (_start != null)
                {
                    return _start.Invoke(c);
                }

                return TaskStatus.Continue; // Start is not required, so report back Continue if we have no Start func.
            }

            throw new Exception("Unexpected context type!");
        }

        public TaskStatus Update(IContext ctx)
        {
            if (ctx is T c)
            {
                return _func?.Invoke(c) ?? TaskStatus.Failure;
            }

            throw new Exception("Unexpected context type!");
        }

        public void Stop(IContext ctx)
        {
            if (ctx is T c)
            {
                _funcStop?.Invoke(c);
            }
            else
            {
                throw new Exception("Unexpected context type!");
            }
        }

        public void Abort(IContext ctx)
        {
            if (ctx is T c)
            {
                _funcAborted?.Invoke(c);
            }
            else
            {
                throw new Exception("Unexpected context type!");
            }
        }
    }
}
