namespace FluidHTN
{
    public interface IEffect<TWorldStateEntry>
    {
        string Name { get; }
        EffectType Type { get; }
        void Apply(IContext<TWorldStateEntry> ctx);
    }
}
