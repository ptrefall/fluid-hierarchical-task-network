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
    public override List<string> MTRDebug { get; set; } = null;
    public override List<string> LastMTRDebug { get; set; } = null;
    public override bool DebugMTR { get; } = false;
    public override Stack<string> DecompositionLog { get; set; } = null;
    public override bool LogDecomposition { get; } = false;
        
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
You might notice that we had to override the debug properties. We set the collections to null and the boolean flags to false for now. We will cover debugging later in this document.

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
    planner.Tick(domain, ctx);
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
A powerful feature of Fluid HTN, is how easy it is to extend the domain builder with specialized task types for a project's problem domain.
Bundled with the library, we have generic implementations of Condition, Operator and Effect, making it trivial to add lambda-styled domain definitions, as expressed in the example earlier in this document. These bundled features are just a starting point, however. It's easy to extend the planner with custom conditions, operators and effects, and it might make your domain definitions easier to read, and be more designer friendly.
```C#
var domain = new MyDomainBuilder("Trunk Thumper")
    .Sequence("Attack enemy")
        .IfEnemy()
        .MoveTo(Location.Enemy, Speed.Sprint)
            .SetLocation(Location.Enemy)
            .SetIsTired()
        .End()
        .TrunkSlam()
            .IfLocation(Location.Enemy)
        .End()
    .End()
    .Sequence("Patrol next bridge")
        .FindBridge()
        .End()
        .MoveTo(Location.Bridge, Speed.Walk)
            .SetLocation(Location.Bridge)
        .End()
        .CheckBridge()
            .IfLocation(Location.Bridge)
            .SetBored()
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
#### Custom effect in domain builder
To add a custom effect, we need to override the IEffect interface.
```C#
public class SetLocationEffect : IEffect
{
    private Location _location;
    
    public SetLocation(Location location)
    {
        _location = location;
    }
    
    public string Name { get; } = "Set Location";
    public EffectType Type { get; } = EffectType.PlanOnly;
    
    public void Apply(IContext ctx)
    {
        if (ctx is T c)
            c.SetState(WorldState.Location, _location);
        else
            throw new Exception("Unexpected context type!");
    }
}
```
Note how this is a plan-only effect. This is because we predict that we will arrive at this location when we attempt to move there, but we actually don't know whether we ever get there during plan execution. In the MoveTo example below we continue the task until we arrive at the destination, but there are cases where we'd want to just set the destination and return success for movement, as we might want to perform other tasks while moving. Because of this, we rely on some system external from this effect to set the state on arrival.

Next, we can extend our MyDomainBuilder with a new function that expose this effect
```C#
public MyDomainBuilder SetLocation(Location location)
{
    if(Pointer is IPrimitiveTask task)
    {
        var effect = new SetLocationEffect(location);
        task.AddEffect(effect);
    }
    else throw new Exception("Tried to add an Effect, but the Pointer is not a Primitive Task!");
    
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
            case Location.Bridge:
                // ...
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
Next, we can extend our MyDomainBuilder with a new function that expose this operator
```C#
public MyDomainBuilder MoveTo(Location location, Speed speed)
{
    Action($"MoveTo({location}, {speed})");
    
    if(Pointer is IPrimitiveTask task)
    {
        var op = new MoveToOperator(location, speed);
        task.SetOperator(op);
    }
    else throw new Exception("Tried to add an Operator, but the Pointer is not a Primitive Task!");
    
    return this;
}
```
Note that we both called Action(...), which sets the Pointer, and  the SetOperator(...), but we didn't call End() to close the Pointer. This is so that we could be free to add Effects and Conditions to the action, but it means that the user must remember to call End() manually.
#### Custom selectors in domain builder
We're not limited to extend the domain builder with just conditions, effects and operators. We can also extend the capabilities of our selectors and sequences. Let's implement a Random Selector that will decompose into a random sub-task.
```C#
using System;
using System.Collections.Generic;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
    public class RandomSelector : Selector
    {
        protected Random _random = new Random();

        protected override Queue<ITask> OnDecompose(IContext ctx, int startIndex)
        {
                Plan.Clear();

                var taskIndex = _random.Next(startIndex, Children.Count - 1);
                var task = Children[taskIndex];

                if (task.IsValid(ctx) == false)
                    return Plan;

                if (task is ICompoundTask compoundTask)
                {
                    var result = compoundTask.Decompose(ctx, 0);

                    // If result is null, that means the entire planning procedure should cancel.
                    if (result == null) return null;

                    // If the decomposition failed
                    if (result.Count == 0) return Plan;

                    while (result.Count > 0)
                    {
                        var res = result.Dequeue();
                        Plan.Enqueue(res);
                    }
                }
                else if (task is IPrimitiveTask primitiveTask)
                {
                    primitiveTask.ApplyEffects(ctx);
                    Plan.Enqueue(task);
                }

                return Plan;
        }
    }
}
```
We can now extend our MyDomainBuilder with this new type of selector.
```C#
public DB RandomSelect(string name)
{
    return CompoundTask<RandomSelector>(name);
}
```
### Debugging the planner
Sometimes we need to see what's going on under the hood to understand why the planner ends up with the plans we are given.
We have some debug options in our context definition, as mentioned earlier. We can set LogDecomposition to true. What this does, is it allows our planning procedure to store information into our context about condition success and failure during decomposition. This can be a big help in understanding how the domain was decomposed into a plan. We can then read out the logs from DecompositionLog in our context. BaseContext will attempt to instantiate the debug collections automatically if the debug flags are set to true when its Init() function is called.
```C#
while(ctx.DecompositionLog.Count > 0)
{
    var log = ctx.DecompositionLog.Pop();
    Console.WriteLine(log);
}
```
The planning system will encode our traversal through the HTN domain as we search for a plan. This method traversal record (MTR) simply stores the method index chosen for each selector that was decomposed to create the plan, recording branching in our decomposition. We can set our context up so that the planner will also provide us with a debug version of this traversal record, which record more information. We simply set DebugMTR to true in our context.
```C#
foreach(var log in ctx.MTRDebug)
{
    Console.WriteLine(log);
}
```
When DebugMTR is true, we will also track the previous traversal record in LastMTRDebug. This can be useful to compare the current and previous traversal record when a plan was replaced, for instance.
```C#
foreach(var log in ctx.LastMTRDebug)
{
    Console.WriteLine(log);
}
```
The reason these debug properties are all abstract in BaseContext, is because Fluid HTN must be generic enough to be used varied environments. In Unity, for instance, a user might want to have these debug flags enabled only when in the editor, or when running the game in a special dev-mode. Or maybe the user doesn't use Unity at all, and other policies are applied for when to debug.
#### Callback hooks in the planner
Sometimes these debug logs won't be enough to understand how the planner flows and gives us the results it does. Or maybe there is a need to hook up to certain events in the planner for other purposes. The planner exposes multiple callbacks that we can hook up to.
```C#
/// <summary>
/// OnPreReplacePlan(oldPlan, newPlan) is called when we're about to replace the
/// current plan with a new plan. The current plan might be empty / completed.
/// </summary>
public Action<Queue<ITask>, Queue<ITask>> OnPreReplacePlan = null;
```

### Using Fluid HTN with Unity
In UnityProject/Packages/manifest.json add the following line under dependencies, and edit the path to point to where you have cloned the Fluid HTN repository.
```json
"fluid.htn": "file:path/to/fluid-hierarchial-task-network/FluidHTN"
```
Your Unity project should now have integrated Fluid HTN via the Package Manager, and you should be able to proceed with the Getting Started example above. Slightly more elaborate examples based on Unity is also available in the Examples section below.

If preferred, the FluidHTN folder of the planner can also be copy/pasted somewhere into your Unity project's Assets folder.

## Extensions
The [Fluid HTN Extension library](https://github.com/ptrefall/fluid-hierarchial-task-network-ext) adds extended selector implementations, like Random Select and Utility Select, as well as JSON serialization of HTN Domains.

## Examples
Example projects have been pulled into their own repositories, as not to clutter the core library.

## TODO
* Improve documentation
