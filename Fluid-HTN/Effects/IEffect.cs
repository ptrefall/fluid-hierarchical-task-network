namespace FluidHTN
{
    public interface IEffect
    {
        string Name { get; }
        int Depth { get; set; }
        EffectType Type { get; }
        void Apply(IContext ctx);
    }
}
