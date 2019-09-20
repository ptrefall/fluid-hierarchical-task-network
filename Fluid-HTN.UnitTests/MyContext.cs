using System;
using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Contexts;
using FluidHTN.Debug;
using FluidHTN.Factory;

public enum MyWorldState : byte
{
    HasA,
    HasB,
    HasC
}

internal class MyContext : BaseContext<byte>
{
    private byte[] _worldState = new byte[Enum.GetValues(typeof(MyWorldState)).Length];
    public override IFactory Factory { get; set; } = new DefaultFactory();
    public override List<string> MTRDebug { get; set; } = null;
    public override List<string> LastMTRDebug { get; set; } = null;
    public override bool DebugMTR { get; } = false;
    public override Queue<IBaseDecompositionLogEntry> DecompositionLog { get; set; } = null;
    public override bool LogDecomposition { get; } = false;
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
        return HasState((int) state, (byte) (value ? 1 : 0));
    }

    public bool HasState(MyWorldState state)
    {
        return HasState((int) state, 1);
    }

    public void SetState(MyWorldState state, bool value, EffectType type)
    {
        SetState((int) state, (byte) (value ? 1 : 0), true, type);
    }

    public byte GetState(MyWorldState state)
    {
        return GetState((int) state);
    }
}

internal class MyDebugContext : MyContext
{
    public override bool DebugMTR { get; } = true;

    public override bool LogDecomposition { get; } = true;
}
