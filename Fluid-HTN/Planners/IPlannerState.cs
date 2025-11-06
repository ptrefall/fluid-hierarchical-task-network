using FluidHTN.Conditions;
using FluidHTN.PrimitiveTasks;
using System;
using System.Collections.Generic;

namespace FluidHTN
{
    public interface IPlannerState
    {
        // ========================================================= PROPERTIES

        ITask CurrentTask { get; set; }
        Queue<ITask> Plan { get; set; }
        TaskStatus LastStatus { get; set; }

        // ========================================================= CALLBACKS

        /// <summary>
        ///		OnNewPlan(newPlan) is called when we found a new plan, and there is no
        ///		old plan to replace.
        /// </summary>
        Action<Queue<ITask>> OnNewPlan { get; set; }

        /// <summary>
        ///		OnReplacePlan(oldPlan, currentTask, newPlan) is called when we're about to replace the
        ///		current plan with a new plan.
        /// </summary>
        Action<Queue<ITask>, ITask, Queue<ITask>> OnReplacePlan { get; set; }

        /// <summary>
        ///		OnNewTask(task) is called after we popped a new task off the current plan.
        /// </summary>
        Action<ITask> OnNewTask { get; set; }

        /// <summary>
        ///		OnNewTaskConditionFailed(task, failedCondition) is called when we failed to
        ///		validate a condition on a new task.
        /// </summary>
        Action<ITask, ICondition> OnNewTaskConditionFailed { get; set; }

        /// <summary>
        ///		OnStopCurrentTask(task) is called when the currently running task was stopped
        ///		forcefully.
        /// </summary>
        Action<IPrimitiveTask> OnStopCurrentTask { get; set; }

        /// <summary>
        ///		OnCurrentTaskCompletedSuccessfully(task) is called when the currently running task
        ///		completes successfully, and before its effects are applied.
        /// </summary>
        Action<IPrimitiveTask> OnCurrentTaskCompletedSuccessfully { get; set; }

        /// <summary>
        ///		OnApplyEffect(effect) is called for each effect of the type PlanAndExecute on a
        ///		completed task.
        /// </summary>
        Action<IEffect> OnApplyEffect { get; set; }

        /// <summary>
        ///		OnCurrentTaskFailed(task) is called when the currently running task fails to complete.
        /// </summary>
        Action<IPrimitiveTask> OnCurrentTaskFailed { get; set; }

        /// <summary>
        ///		OnCurrentTaskStarted(task) is called once when a new task in the plan is selected.
        /// </summary>
        Action<IPrimitiveTask> OnCurrentTaskStarted { get; set; }

        /// <summary>
        ///		OnCurrentTaskContinues(task) is called every tick that a currently running task
        ///		needs to continue.
        /// </summary>
        Action<IPrimitiveTask> OnCurrentTaskContinues { get; set; }

        /// <summary>
        ///		OnCurrentTaskExecutingConditionFailed(task, condition) is called if an Executing Condition
        ///		fails. The Executing Conditions are checked before every call to task.Operator.Update(...).
        /// </summary>
        Action<IPrimitiveTask, ICondition> OnCurrentTaskExecutingConditionFailed { get; set; }
    }
}
