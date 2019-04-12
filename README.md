# Fluid Hierarchial Task Network
A simple HTN planner based around the principles of the Builder pattern, inspired by [Fluid Behaviour Tree](https://github.com/ashblue/fluid-behavior-tree).

## Features
* Comes with a Domain Builder to simplify the design of code-oriented HTN domains.
* Fluid HTN is a total-order forward decomposition planner, as described by Troy Humphrey in his [GameAIPro article](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf).
* Partial planning
* Domain splicing
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
    public bool Done { get; set; } = false;
    
    public override void Init()
    {
        base.Init();
        
        // Custom init of state
    }
}
```
Out of convenience we extend our context with some specialized world state manipulation methods now that we have defined our world state.
```C#
public class MyContext : BaseContext
    {
        // ...

        public bool HasState(MyWorldState state, bool value)
        {
            return HasState((int)state, (byte) (value ? 1 : 0));
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
```
Now we have what we need to start to define a new HTN domain.
```C#
var domain = new DomainBuilder<MyContext>("MyDomain")
    .Select("C")
        .Condition("Has A and B", (ctx) => ctx.HasState(MyWorldState.HasA) && ctx.HasState(MyWorldState.HasB))
        .Condition("Has NOT C", (ctx) => !ctx.HasState(MyWorldState.HasC))
        .Action("Get C")
            .Do((ctx) => { Console.WriteLine("Get C"); return TaskStatus.Success; })
            .Effect("Has C", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(MyWorldState.HasC, true, type))
        .End()
    .End()
    .Sequence("A and B")
        .Condition("Has NOT A nor B", (ctx) => !(ctx.HasState(MyWorldState.HasA) && ctx.HasState(MyWorldState.HasB)))
        .Action("Get A")
            .Do((ctx) => { Console.WriteLine("Get A"); return TaskStatus.Success; })
            .Effect("Has A", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(MyWorldState.HasA, true, type))
        .End()
        .Action("Get B")
            .Condition("Has A", (ctx) => ctx.HasState(MyWorldState.HasA))
            .Do((ctx) => { Console.WriteLine("Get B"); return TaskStatus.Success; })
            .Effect("Has B", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(MyWorldState.HasB, true, type))
        .End()
    .End()
    .Select("Done")
        .Action("Done")
            .Do((ctx) => 
            {
                Console.WriteLine("Done");
                ctx.Done = true;
                return TaskStatus.Continue;
            })
        .End()
    .End()
    .Build();
```
Now that we have a domain, we can start to generate plans. First we need to instantiate our planner and the context.
```C#
var ctx = new MyContext();
var planner = new Planner();
ctx.Init();
```
Next, let's tick the planner until the Done flag in our context is set to false.
```C#
while (!ctx.Done)
{
    planner.TickPlan(domain, ctx);
}
```
Now, if we run this example, we should see the following print to our console:
```
Get A
Get B
Get C
Done

```
### Partial planning
We can easily integrate the concept of partial planning into our domains. We call it a Pause Plan, and it must be set inside a sequence to be valid. It allows the planner to only plan up to a certain point, then continue from there once the partial plan has been completed.
```C#
.Sequence("A")
    .Action("1")
        //...
    .End()
    .PausePlan()
    .Action("2")
        //...
    .End()
.End()
```
### Sub-domains and domain splicing
We can define sub-domains and splice them together to form new domains, but they must share the same context type to be compatible. This can be quite handy for re-use of sub-domains, and prevent a single domain definition from growing too large.
```C#
var subDomain = new DomainBuilder<MyContext>("SubDomain")
    .Select("B")
        //...
    .End()
    .Build();
    
var myDomain = new DomainBuilder<MyContext>("MyDomain")
    .Select("A")
        //...
    .End()
    .Splice(subDomain)
    .Select("C")
        //...
    .End()
    .Build();
```
### Extending the Domain Builder
A powerful feature of the Fluid HTN, is how easy it is to extend the domain builder with specialized task types for a project's problem domain.
Bundled with the library, we have generic implementations of Condition, Operator and Effect, making it trivial to add lambda-styled domain definitions, as expressed in the example earlier in this document. These bundled features are just a starting point, however. It's easy to extend the planner with custom conditions, operators and effects, and it might make your domain definitions easier to read, and be more designer friendly.
```C#
var domain = new MyDomainBuilder("Trunk Thumper")
    .Sequence("Attack enemy")
        .IfEnemy()
        .MoveTo(Location.Enemy, Speed.Sprint)
            .AtLocation(Location.Enemy)
            .IsTired()
        .End()
        .TrunkSlam()
            .IfLocation(Location.Enemy)
        .End()
    .End()
    .Sequence("Patrol next bridge")
        .FindBridge()
        .End()
        .MoveTo(Location.Bridge, Speed.Walk)
            .AtLocation(Location.Bridge)
        .End()
        .CheckBridgeAction()
            .IfLocation(Location.Bridge)
            .GetBored()
        .End()
    .End()
    .Build();
```
Let's look at how parts of this was made. First, we write our custom Domain Builder class.
```C#
public class MyDomainBuilder : BaseDomainBuilder<MyDomainBuilder, MyContext>
{
    public MyDomainBuilder(string domainName) : base(domainName)
    {
    }
}
```
#### Custom condition in domain builder
To add a custom condition, we need to override the ICondition interface.
```C#
public class IfEnemyCondition : ICondition
{
    public string Name { get; } = "If Enemy";
    
    public bool IsValid(ICondition ctx)
    {
        if(ctx is MyContext c)
        {
            return c.HasState(WorldState.HasEnemy);
        }
        
        throw new Exception("Unexpected context type!");
    }
}
```
Next, we can extend our MyDomainBuilder with a new function that expose this condition
```C#
public MyDomainBuilder IfEnemy()
{
    var condition = new IfEnemyCondition();
    Pointer.AddCondition(condition);
    
    return this;
}
```
#### Custom operator in domain builder
To add a custom operator, we need to override the IOperator interface.
```C#
public class MoveToOperator : IOperator
{
    private Location _location;
    private Speed _speed;
    
    public MoveToOperator(Location location, Speed speed)
    {
        _location = location;
        _speed = speed;
    }
    
    public TaskStatus Update(IContext ctx)
    {
        if(ctx is MyContext c) 
        {
            if(c.NavAgent.isStopped)
                return InitiateMovement(c);
                
            return TickMovement(c);
        }
        throw new Exception("Unexpected context type!");
    }
    
    public void Stop(IContext ctx)
    {
        if(ctx is MyContext c) 
        {
            c.NavAgent.isStopped = true;
        }
        throw new Exception("Unexpected context type!");
    }
    
    private TaskStatus InitiateMovement(MyContext c)
    {
        c.NavAgent.speed = _speed == Speed.Walk ? WalkSpeed : RunSpeed;
        switch(_location)
        {
            case Location.Enemy:
                if (ctx.NavAgent.SetDestination(ctx.BridgeLocation))
                {
                    ctx.NavAgent.isStopped = false;
                    return TaskStatus.Continue;
                }
                else
                    return TaskStatus.Failure;
        }
        return TaskStatus.Failure;
    }
    
    private TaskStatus TickMovement(MyContext c)
    {
        if(c.NavAgent.remainingDistance > c.NavAgent.stoppingDistance)
            return TaskStatus.Continue;
        
        c.NavAgent.isStopped = true;
        return TaskStatus.Success;
    }
}
```
### Using Fluid HTN with Unity
In UnityProject/Packages/manifest.json add the following line under dependencies, and edit the path to point to where you have cloned the Fluid HTN repository.
```json
"fluid.htn": "file:path/to/fluid-hierarchial-task-network"
```
Your Unity project should now have integrated Fluid HTN, and you should be able to proceed with the getting started example above. Slightly more elaborate examples based on Unity is also available in the Examples section below.

## Extensions
The [Fluid HTN Extension library](https://github.com/ptrefall/fluid-hierarchial-task-network-ext) adds extended selector implementations, like Random Select and Utility Select, as well as JSON serialization of HTN Domains.

## Examples
Example projects have been pulled into their own repositories, as not to clutter the core library.

## TODO
* Improve documentation
