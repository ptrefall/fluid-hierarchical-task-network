namespace FluidHTN.Conditions
{
    public interface ICondition
    {
        string Name { get; }
        int Depth { get; set; }
        bool IsValid(IContext ctx);
    }
}
