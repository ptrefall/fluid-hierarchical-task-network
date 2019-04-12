using System;
using FluidHTN;
using FluidHTN.Contexts;

public enum MyWorldState : byte
{
    HasA,
    HasB,
    HasC
}

public class MyContext : BaseContext
{
    private byte[] _worldState = new byte[Enum.GetValues(typeof(MyWorldState)).Length];
    public override byte[] WorldState => _worldState;

    // Custom state
    public bool Done { get; set; } = false;

    public override void Init()
    {
        base.Init();

        // Custom init of state
    }

    public bool HasState(MyWorldState state, bool value)
    {
        return HasState((int)state, (byte)(value ? 1 : 0));
    }

    public bool HasState(MyWorldState state)
    {
        return HasState((int)state, 1);
    }

    public void SetState(MyWorldState state, bool value, EffectType type)
    {
        SetState((int)state, (byte)(value ? 1 : 0), true, type);
    }
}