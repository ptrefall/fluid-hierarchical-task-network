namespace FluidHTN.Operators
{
    public interface IOperator<TWorldStateEntry>
    {
        TaskStatus Update(IContext<TWorldStateEntry> ctx);
        void Stop(IContext<TWorldStateEntry> ctx);
    }
}
