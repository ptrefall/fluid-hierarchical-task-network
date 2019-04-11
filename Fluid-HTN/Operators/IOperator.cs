namespace FluidHTN.Operators
{
    public interface IOperator
    {
        TaskStatus Update(IContext ctx);
        void Stop(IContext ctx);
    }
}