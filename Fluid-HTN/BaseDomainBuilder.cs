using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.Factory;
using FluidHTN.Operators;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN
{
    public abstract class BaseDomainBuilder<DB, T>
        where DB : BaseDomainBuilder<DB, T>
        where T : IContext
    {
        // ========================================================= FIELDS

        protected readonly Domain<T> _domain;
        protected List<ITask> _pointers;
        protected readonly IFactory _factory;

        // ========================================================= CONSTRUCTION

        public BaseDomainBuilder(string domainName, IFactory factory)
        {
            _factory = factory;
            _domain = new Domain<T>(domainName);
            _pointers = _factory.CreateList<ITask>();
            _pointers.Add(_domain.Root);
        }

        // ========================================================= PROPERTIES

        public ITask Pointer
        {
            get
            {
                if (_pointers.Count == 0) return null;
                return _pointers[_pointers.Count - 1];
            }
        }

        // ========================================================= HIERARCHY HANDLING

        /// <summary>
        ///     Compound tasks are where HTN get their “hierarchical” nature. You can think of a compound task as 
        ///     a high level task that has multiple ways of being accomplished. There are primarily two types of 
        ///     compound tasks. Selectors and Sequencers. A Selector must be able to decompose a single sub-task, 
        ///     while a Sequence must be able to decompose all its sub-tasks successfully for itself to have decomposed 
        ///     successfully. There is nothing stopping you from extending this toolset with RandomSelect, UtilitySelect,
        ///     etc. These tasks are decomposed until we're left with only Primitive Tasks, which represent a final plan. 
        ///     Compound tasks are comprised of a set of subtasks and a set of conditions.
        ///     http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
        /// </summary>
        /// <typeparam name="P">The type of compound task</typeparam>
        /// <param name="name">The name given to the task, mainly for debug/display purposes</param>
        /// <returns></returns>
        public DB CompoundTask<P>(string name) where P : ICompoundTask, new()
        {
            var parent = new P();
            return CompoundTask(name, parent);
        }

        /// <summary>
        ///     Compound tasks are where HTN get their “hierarchical” nature. You can think of a compound task as 
        ///     a high level task that has multiple ways of being accomplished. There are primarily two types of 
        ///     compound tasks. Selectors and Sequencers. A Selector must be able to decompose a single sub-task, 
        ///     while a Sequence must be able to decompose all its sub-tasks successfully for itself to have decomposed 
        ///     successfully. There is nothing stopping you from extending this toolset with RandomSelect, UtilitySelect,
        ///     etc. These tasks are decomposed until we're left with only Primitive Tasks, which represent a final plan. 
        ///     Compound tasks are comprised of a set of subtasks and a set of conditions.
        ///     http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
        /// </summary>
        /// <typeparam name="P">The type of compound task</typeparam>
        /// <param name="name">The name given to the task, mainly for debug/display purposes</param>
        /// <param task="task">The task instance</param>
        /// <returns></returns>
        public DB CompoundTask<P>(string name, P task) where P : ICompoundTask
        {
            if (task != null)
            {
                if (Pointer is ICompoundTask compoundTask)
                {
                    task.Name = name;
                    _domain.Add(compoundTask, task);
                    _pointers.Add(task);
                }
                else
                {
                    throw new Exception(
                        "Pointer is not a compound task type. Did you forget an End() after a Primitive Task Action was defined?");
                }
            }
            else
            {
                throw new ArgumentNullException(
                    "task");
            }

            return (DB) this;
        }

        /// <summary>
        ///     Primitive tasks represent a single step that can be performed by our AI. A set of primitive tasks is 
        ///     the plan that we are ultimately getting out of the HTN. Primitive tasks are comprised of an operator, 
        ///     a set of effects, a set of conditions and a set of executing conditions.
        ///     http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
        /// </summary>
        /// <typeparam name="P">The type of primitive task</typeparam>
        /// <param name="name">The name given to the task, mainly for debug/display purposes</param>
        /// <returns></returns>
        public DB PrimitiveTask<P>(string name) where P : IPrimitiveTask, new()
        {
            if (Pointer is ICompoundTask compoundTask)
            {
                var parent = new P { Name = name };
                _domain.Add(compoundTask, parent);
                _pointers.Add(parent);
            }
            else
            {
                throw new Exception(
                    "Pointer is not a compound task type. Did you forget an End() after a Primitive Task Action was defined?");
            }

            return (DB) this;
        }

        /// <summary>
        ///     Partial planning is one of the most powerful features of HTN. In simplest terms, it allows
        ///     the planner the ability to not fully decompose a complete plan. HTN is able to do this because
        ///     it uses forward decomposition or forward search to find plans. That is, the planner starts with
        ///     the current world state and plans forward in time from that. This allows the planner to only
        ///     plan ahead a few steps.
        ///     http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
        /// </summary>
        /// <returns></returns>
        protected DB PausePlanTask()
        {
            if (Pointer is IDecomposeAll compoundTask)
            {
                var parent = new PausePlanTask() { Name = "Pause Plan" };
                _domain.Add(compoundTask, parent);
            }
            else
            {
                throw new Exception(
                    "Pointer is not a decompose-all compound task type, like a Sequence. Maybe you tried to Pause Plan a Selector, or forget an End() after a Primitive Task Action was defined?");
            }

            return (DB) this;
        }

        // ========================================================= COMPOUND TASKS

        /// <summary>
        ///     A compound task that requires all sub-tasks to be valid.
        ///     Sub-tasks can be sequences, selectors or actions.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DB Sequence(string name)
        {
            return CompoundTask<Sequence>(name);
        }

        /// <summary>
        ///     A compound task that requires a single sub-task to be valid.
        ///     Sub-tasks can be sequences, selectors or actions.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DB Select(string name)
        {
            return CompoundTask<Selector>(name);
        }

        // ========================================================= PRIMITIVE TASKS

        /// <summary>
        ///     A primitive task that can contain conditions, an operator and effects.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DB Action(string name)
        {
            return PrimitiveTask<PrimitiveTask>(name);
        }

        // ========================================================= CONDITIONS

        /// <summary>
        ///     A precondition is a boolean statement required for the parent task to validate.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public DB Condition(string name, Func<T, bool> condition)
        {
            var cond = new FuncCondition<T>(name, condition);
            Pointer.AddCondition(cond);

            return (DB) this;
        }

        /// <summary>
        ///     An executing condition is a boolean statement validated before every call to the current
        ///		primitive task's operator update tick. It's only supported inside primitive tasks / Actions.
        ///		Note that this condition is never validated during planning, only during execution.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public DB ExecutingCondition(string name, Func<T, bool> condition)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var cond = new FuncCondition<T>(name, condition);
                task.AddExecutingCondition(cond);
            }
            else
            {
                throw new Exception("Tried to add an Executing Condition, but the Pointer is not a Primitive Task!");
            }

            return (DB) this;
        }

        // ========================================================= OPERATORS

        /// <summary>
        ///     The operator of an Action / primitive task.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DB Do(Func<T, TaskStatus> action, Action<T> forceStopAction = null)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var op = new FuncOperator<T>(action, forceStopAction);
                task.SetOperator(op);
            }
            else
            {
                throw new Exception("Tried to add an Operator, but the Pointer is not a Primitive Task!");
            }

            return (DB) this;
        }

        // ========================================================= EFFECTS

        /// <summary>
        ///     Effects can be added to an Action / primitive task.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="effectType"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public DB Effect(string name, EffectType effectType, Action<T, EffectType> action)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new ActionEffect<T>(name, effectType, action);
                task.AddEffect(effect);
            }
            else
            {
                throw new Exception("Tried to add an Effect, but the Pointer is not a Primitive Task!");
            }

            return (DB) this;
        }

        // ========================================================= OTHER OPERANDS

        /// <summary>
        ///     Every task encapsulation must end with a call to End(), otherwise subsequent calls will be applied wrong.
        /// </summary>
        /// <returns></returns>
        public DB End()
        {
            _pointers.RemoveAt(_pointers.Count - 1);
            return (DB) this;
        }

        /// <summary>
        ///     We can splice multiple domains together, allowing us to define reusable sub-domains.
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public DB Splice(Domain<T> domain)
        {
            if (Pointer is ICompoundTask compoundTask)
                _domain.Add(compoundTask, domain.Root);
            else
                throw new Exception(
                    "Pointer is not a compound task type. Did you forget an End()?");

            return (DB) this;
        }

        /// <summary>
        ///     The identifier associated with a slot can be used to splice 
        ///     sub-domains onto the domain, and remove them, at runtime.
        ///     Use TrySetSlotDomain and ClearSlot on the domain instance at
        ///     runtime to manage this feature. SlotId can typically be implemented
        ///     as an enum.
        /// </summary>
        public DB Slot(int slotId)
        {
            if (Pointer is ICompoundTask compoundTask)
            {
                var slot = new Slot() { SlotId = slotId, Name = $"Slot {slotId}" };
                _domain.Add(compoundTask, slot);
            }
            else
                throw new Exception(
                    "Pointer is not a compound task type. Did you forget an End()?");

            return (DB) this;
        }

        /// <summary>
        ///     We can add a Pause Plan when in a sequence in our domain definition,
        ///     and this will give us partial planning.
        ///     It means that we can tell our planner to only plan up to a certain point,
        ///     then stop. If the partial plan completes execution successfully, the next
        ///     time we try to find a plan, we will continue planning where we left off.
        ///     Typical use cases is to split after we navigate toward a location, since
        ///     this is often time consuming, it's hard to predict the world state when
        ///     we have reached the destination, and thus there's little point wasting
        ///     milliseconds on planning further into the future at that point. We might
        ///     still want to plan what to do when reaching the destination, however, and
        ///     this is where partial plans come into play.
        /// </summary>
        public DB PausePlan()
        {
            return PausePlanTask();
        }

        /// <summary>
        ///     Build the designed domain and return a domain instance.
        /// </summary>
        /// <returns></returns>
        public Domain<T> Build()
        {
            if (Pointer != _domain.Root)
                throw new Exception($"The domain definition lacks one or more End() statements. Pointer is '{Pointer.Name}', but expected '{_domain.Root.Name}'.");

            _factory.FreeList(ref _pointers);
            return _domain;
        }
    }
}
