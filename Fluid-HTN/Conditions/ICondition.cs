namespace FluidHTN.Conditions
{
    public interface ICondition<TWorldStateEntry>
    {
        string Name { get; }
        bool IsValid(IContext<TWorldStateEntry> ctx);
    }
}
