![Fluid Hierarchical Task Network](https://i.imgur.com/xKfIV0f.png)

A simple HTN planner based around the principles of the Builder pattern, inspired by [Fluid Behaviour Tree](https://github.com/ashblue/fluid-behavior-tree). Please expand this readme if you're on mobile for more information.

![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)
![Build status](https://travis-ci.org/ptrefall/fluid-hierarchical-task-network.svg?branch=master)
![Stars](https://img.shields.io/github/stars/ptrefall/fluid-hierarchical-task-network.svg)
![Forks](https://img.shields.io/github/forks/ptrefall/fluid-hierarchical-task-network.svg)
![Issues](https://img.shields.io/github/issues/ptrefall/fluid-hierarchical-task-network.svg)

## Features
* Fluid HTN is a total-order forward decomposition planner, as described by Troy Humphreys in his [GameAIPro article](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf).
* Comes with a Domain Builder to simplify the design of code-oriented HTN domains.
* Partial planning.
* Domain splicing.
* Domain slots for run-time splicing.
* Replan only when plans complete/fail or when world state change.
* Early rejection of replanning that can't win.
* Easy to extend with new features, as demonstrated in the [extension library](https://github.com/ptrefall/fluid-hierarchical-task-network-ext).
* Uses a Factory interface internally to create and free arrays/collections/objects, allowing the user to add pooling, or other memory management schemes.
* Decomposition logging, for debugging.
* Comes with Unity Package Module definitions for seamless integration into Unity projects.
* 143 unit tests.

## Support
Join the [discord channel](https://discord.gg/MuccnAz) to share your experience and get support on the usage of Fluid HTN.

## Getting started
### What is Hierarchical Task Network planning
It is highly recommended to read and watch the following resources on HTN planning before using this planner.
* [Troy Humphreys' GameAIPro article](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf)
* [AI and Games' Horizon Zero Dawn coverage](https://www.youtube.com/watch?v=XxuSFBVQULY)
* [AI and Games' Transformers coverage](https://www.youtube.com/watch?v=kXm467TFTcY)
* [AiGameDev's Planning in games overview](http://aigamedev.com/open/review/planning-in-games/)

If you want an in-depth look into ai planning, the University of Edinburgh has a great series on the topic
* [University of Edinburgh's AI Planning series](https://www.youtube.com/watch?v=7Vy8970q0Xc&list=PLwJ2VKmefmxpUJEGB1ff6yUZ5Zd7Gegn2)
### Library concepts
#### Compound Tasks
Compound tasks are where HTN get their “hierarchical” nature. You can think of a compound task as a high level task that has multiple ways of being accomplished. There are primarily two types of compound tasks. Selectors and Sequencers. A Selector must be able to decompose a single sub-task, while a Sequence must be able to decompose all its sub-tasks successfully for itself to have decomposed successfully. There is nothing stopping you from extending this toolset with RandomSelect, UtilitySelect, etc. These tasks are decomposed until we're left with only Primitive Tasks, which represent a final plan. Compound tasks are comprised of a set of subtasks and a set of conditions.
#### Primitive Tasks
Primitive tasks represent a single step that can be performed by our AI.  A set of primitive tasks is the plan that we are ultimately getting out of the HTN. Primitive tasks are comprised of an operator, a set of effects, a set of conditions and a set of executing conditions.
#### Conditions
Conditions are boolean validators that can be used to validate the decomposition of a compound task, or the validity of a primitive task. Primitive Tasks also have Executing Conditions, which we validate before every update to the primary task's operator during execution of a plan.
#### Operators
Operators are the logic operation a primitive task should perform during plan execution. Every time an operator updates, it returns a status whether it succeeded, failed or need to continue next tick.
#### Effects
Effects apply world state change during planning, and optionally during execution. They can only be applied to primitive tasks. There are three types of effects. 
* PlanOnly effects temporarily change the world state during planning, used as a prediction about the future. Its change on the world state is removed before plan execution. This can be useful when we need other systems to set the world state during execution.
* PlanAndExecute effects work just like PlanOnly effects, only that during execution, when the task they represent complete its execution successfully, the effect is re-applied. This is useful in the cases where you don't have other systems to set the world state during execution.
* Permanent effects are applied during planning, but not removed from the world state before execution. This can be very useful when there's some state we change only during planning, e.g. do this thing three times then do this other thing.
### Coding with Fluid HTN
First we need to set up a WorldState enum and a Context. This is the blackboard the planner uses to access state during its planning procedure.
```C#
using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Contexts;
using FluidHTN.Factory;

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
    public override Queue<IBaseDecompositionLogEntry> DecompositionLog { get; set; } = null;
    public override bool LogDecomposition { get; } = false;
    
    public override IFactory Factory { get; set; } = new DefaultFactory();
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
You might notice that we had to override the debug properties. We set the collections to null and the boolean flags to false for now. We will cover debugging later.

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
Now that we have a domain, we can start to generate a plan. First, we need to instantiate our planner and the context.
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
We can define sub-domains and splice them together to form new domains, but they must share the same context type to be compatible. This can be quite handy for re-use of sub-domains and prevent a single domain definition from growing too large. Specially if we want to form recursive-style behavior this is needed.
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
### Sub-domains and slots
We can define slots in our domain definitions, and mark them with slot ids. This allow us to hook up sub-domains to these slots at run-time. This can be useful together with Smart Objects that extend the behavior of an agent when interacted with, for example.
```C#
var subDomain = new DomainBuilder<MyContext>("SubDomain")
    .Select("B")
        //...
    .End()
    .Build();

var myDomain = new DomainBuilder<MyContext>("MyDomain")
    .Slot(1)
    .Build();
    
myDomain.TrySetSlotDomain(1, subDomain);
//...
myDomain.ClearSlot(1);
```
### Extending the Domain Builder
A powerful feature of Fluid HTN, is how easy it is to extend the domain builder with specialized task types for a project's problem domain.
Bundled with the library, we have generic implementations of Condition, Operator and Effect, making it trivial to add lambda-styled domain definitions, as expressed in the example earlier. These bundled features are just a starting point, however. It's easy to extend the planner with custom conditions, operators and effects, and it can make your domain definitions easier to read and work with.
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
Let us look at how parts of this was made. First, we write our custom Domain Builder class. We instantiate a DefaultFactory into base, but we'll cover custom factory implementations later.
```C#
public class MyDomainBuilder : BaseDomainBuilder<MyDomainBuilder, MyContext>
{
    public MyDomainBuilder(string domainName) : base(domainName, new DefaultFactory())
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
    
    public bool IsValid(IContext ctx)
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
            return;
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
Next, we can extend our MyDomainBuilder with a new function that expose this operator.
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
Note that we both called Action(...), which sets the Pointer, and the SetOperator(...), but we didn't call End() to close the Pointer. This is so that we could be free to add Effects and Conditions to the action, but it means that the user must remember to call End() manually.
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

        DecompositionStatus OnDecompose(IContext ctx, int startIndex, out Queue<ITask> result)
        {
            Plan.Clear();

            var taskIndex = _random.Next(startIndex, Subtasks.Count);
            var task = Subtasks[taskIndex];

            return OnDecomposeTask(ctx, task, taskIndex, null, out result);
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
#### Custom factory
When we implemented MyContext earlier, you might have noticed that we did an override to implement Factory, and set it to DefaultFactory. We also sent a DefaultFactory to BaseDomainBuilder when we looked at extending domain builders. This is where you're free to implement your own factory methods, like PooledFactory, and have Fluid HTN use it via the IFactory interface. DefaultFactory will just do normal new operations and set the collection reference to null when the Free* API is called. The Create* and Free* API of the IFactory is used internally with the support of pooling in mind, but we leave it up to the user how they prefer to do this.

### Debugging the planner
Sometimes we need to see what's going on under the hood to understand why the planner ends up with the plans we are given.
We have some debug options in our context definition, as mentioned earlier. We can set LogDecomposition to true. What this does, is it allows our planning procedure to store information into our context about condition success and failure during decomposition. This can be a big help in understanding how the domain was decomposed into a plan. We can then read out the logs from DecompositionLog in our context. BaseContext will attempt to instantiate the debug collections automatically if the debug flags are set to true when its Init() function is called.
```C#
if (_context.LogDecomposition)
{
    while (_context.DecompositionLog?.Count > 0)
    {
        var entry = _context.DecompositionLog.Dequeue();
        var depth = FluidHTN.Debug.Debug.DepthToString(entry.Depth);
        Console.ForegroundColor = entry.Color;
        Console.WriteLine($"{depth}{entry.Name}: {entry.Description}");
    }
    Console.ResetColor();
}
```
We can take further advantage of the decomposition log if we apply context log calls to our custom extensions. While the task implementation in Fluid HTN already has extensive decomposition logging support integrated that should cover most requirements, our custom conditions and effects could benefit from adding a custom log. Let's improve our custom condition and effect from earlier, by applying decomposition logging to them.
```C#
public class IfEnemyCondition : ICondition
{
    public string Name { get; } = "If Enemy";
    
    public bool IsValid(ICondition ctx)
    {
        if(ctx is MyContext c)
        {
            var result = c.HasState(WorldState.HasEnemy);
            if (ctx.LogDecomposition) ctx.Log(Name, $"IfEnemyCondition.IsValid:{result}", ctx.CurrentDecompositionDepth+1, this, result ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
            return result;
        }
        
        throw new Exception("Unexpected context type!");
    }
}
```
```C#
public class SetLocationEffect : IEffect
{
    private Location _location;
    
    public SetLocation(Location location)
    {
        _location = location;
        Name = $"Set Location[{location}]";
    }
    
    public string Name { get; private set; }
    public EffectType Type { get; } = EffectType.PlanOnly;
    
    public void Apply(IContext ctx)
    {
        if (ctx is T c)
        {
            if (ctx.LogDecomposition) ctx.Log(Name, $"SetLocationEffect.Apply:{Type}", ctx.CurrentDecompositionDepth+1, this);
            c.SetState(WorldState.Location, _location);
        }
        else
            throw new Exception("Unexpected context type!");
    }
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

OnNewPlan(newPlan) is called when we found a new plan, and there is no old plan to replace.
```C#
public Action<Queue<ITask>> OnNewPlan = null;
```
OnReplacePlan(oldPlan, currentTask, newPlan) is called when we're about to replace the current plan with a new plan.
```C#
public Action<Queue<ITask>, ITask, Queue<ITask>> OnReplacePlan = null;
```
OnNewTask(task) is called after we popped a new task off the current plan.
```C#
public Action<ITask> OnNewTask = null;
```
OnNewTaskConditionFailed(task, failedCondition) is called when we failed to validate a condition on a new task.
```C#
public Action<ITask, ICondition> OnNewTaskConditionFailed = null;
```
OnStopCurrentTask(task) is called when the currently running task was stopped forcefully.
```C#
public Action<IPrimitiveTask> OnStopCurrentTask = null;
```
OnCurrentTaskCompletedSuccessfully(task) is called when the currently running task completes successfully, and before its effects are applied.
```C#
public Action<IPrimitiveTask> OnCurrentTaskCompletedSuccessfully = null;
```
OnApplyEffect(effect) is called for each effect of the type PlanAndExecute on a completed task.
```C#
public Action<IEffect> OnApplyEffect = null;
```
OnCurrentTaskFailed(task) is called when the currently running task fails to complete.
```C#
public Action<IPrimitiveTask> OnCurrentTaskFailed = null;
```
OnCurrentTaskContinues(task) is called every tick that a currently running task needs to continue.
```C#
public Action<IPrimitiveTask> OnCurrentTaskContinues = null;
```

### Using Fluid HTN with Unity
In Unity, open the Package Manager via the Windows menu. [Click the Add package from disk button, which allows you to specify the location of an external package](https://docs.unity3d.com/Manual/upm-ui-local.html).

## Extensions
The [Fluid HTN Extension library](https://github.com/ptrefall/fluid-hierarchical-task-network-ext) adds extended selector implementations, like Random Select, Utility Select, Always Succeed decorator, Invert Status decorator and GOAP Sequence. There is also a JSON serialization of HTN Domains in the works.

## Examples
Example projects have been pulled into their own repositories, as not to clutter the core library. More examples are still in progress, so please check back here to see when they become available.
* [Fluid Text Adventure](https://github.com/ptrefall/fluid-text-adventure)
* [Fluid Troll Bridge](https://github.com/ptrefall/fluid-troll-bridge) (requires Unity)
* [Fluid Goap Coffai](https://github.com/ptrefall/fluid-goap-coffai)
* [Fluid Smart Objects](https://github.com/ptrefall/fluid-smart-objects)
* [Fluid Roguelike](https://github.com/ptrefall/fluid_roguelike) (requires Unity, work in progress)

## TODO
Review the [Projects area](https://github.com/ptrefall/fluid-hierarchical-task-network/projects) of this project to get an overview of what's on the todo-list of this project, and which new features are in progress.
