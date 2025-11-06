namespace FluidHTN.Operators
{
    public interface IOperator
    {
        TaskStatus Start(IContext ctx);
        TaskStatus Update(IContext ctx);

        /// <summary>
        /// Graceful end of task execution.
        /// </summary>
        /// <param name="ctx"></param>
        void Stop(IContext ctx);

        /// <summary>
        /// Forced termination of task execution.
        /// </summary>
        /// <param name="ctx"></param>
        void Abort(IContext ctx);
    }
}
