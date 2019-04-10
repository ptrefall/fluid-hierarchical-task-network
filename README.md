# Fluid Hierarchial Task Network
A simple HTN planner based around the principles of the Builder pattern, inspired by [Fluid Behaviour Tree](https://github.com/ashblue/fluid-behavior-tree).

## Features
* Comes with a Domain Builder to simplify the design of code-oriented HTN domains.
* Fluid HTN is a total-order forward decomposition planner, as described by Troy Humphrey in his [GameAIPro article](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf).
* Partial planning
* Easy to extend with new features, as demonstrated in the [extension library](https://github.com/ptrefall/fluid-hierarchial-task-network-ext).
* Comes with Unity Package Module definitions for seamless integration into Unity projects.

## Getting started
### Coding with Fluid HTN
First we need to set up a WorldState enum and a Context. This is the blackboard the planner uses to access state during its planning procedure.
```C#
using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Compounds;
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
    
    public override void Init()
    {
        base.Init();
        
        // Custom init of state
    }
}
```
Now we have what we need to start to define a new HTN domain.
```C#
var domain = new DomainBuilder<MyContext>( "MyDomain" )
    .Select("C")
        .Condition("Has A and B", (ctx) => ctx.HasState( MyWorldState.HasA && MyWorldState.HasB )
        .Condition("Has NOT C", (ctx) => ctx.HasState( !MyWorldState.HasC )
        .Action( "Get C" )
            .Do( (ctx) => {} )
            .Effect( "Has C", EffectType.PlanOnly, (ctx, type) => ctx.SetState((int)MyWorldState.HasC, 1, true, type)
        .End()
    .End()
    .Sequence("A and B")
        .Condition("Has NOT A nor B", (ctx) => ctx.HasState( !(MyWorldState.HasA && MyWorldState.HasB) )
        .Action( "Get A" )
            .Do( (ctx) => {} )
            .Effect( "Has A", EffectType.PlanOnly, (ctx, type) => ctx.SetState((int)MyWorldState.HasA, 1, true, type)
        .End()
        .Action( "Get B" )
            .Do( (ctx) => {} )
            .Effect( "Has B", EffectType.PlanOnly, (ctx, type) => ctx.SetState((int)MyWorldState.HasB, 1, true, type)
        .End()
        .Select("Done")
            .Action( "Done" )
                .Do( (ctx) => {] )
            .End()
        .End()
    .End()
```
Now that we have a domain, we can start to generate plans. We do that through the Planner API.
```C#
var ctx = new MyContext();
var planner = new Planner();
planner.TickPlan(domain, ctx);
```
### Using Fluid HTN with Unity
In UnityProject/Packages/manifest.json add the following line under dependencies, and edit the path to point to where you have cloned the Fluid HTN repository.
```json
"fluid.htn": "file:path/to/fluid-hierarchial-task-network"
```

## Extensions
The [Fluid HTN Extension library](https://github.com/ptrefall/fluid-hierarchial-task-network-ext) adds extended selector implementations, like Random Select and Utility Select, as well as JSON serialization of HTN Domains.

## Examples
Example projects have been pulled into their own repositories, as not to clutter the core library.
