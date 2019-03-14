using System.Collections.Generic;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
	public class Selector : CompoundTask
	{
		public override bool IsValid( IContext ctx )
		{
			// Check that our preconditions are valid first.
			if ( base.IsValid( ctx ) == false )
				return false;

			// Selector requires there to be children to successfully select from.
			if ( Children.Count == 0 )
				return false;

			return true;
		}

		private readonly Queue< ITask > Plan = new Queue< ITask >();

		/// <summary>
		/// In a Selector decomposition, just a single sub-task must be valid and successfully decompose for the Selector to be successfully decomposed.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		protected override Queue<ITask> OnDecompose( IContext ctx )
		{
			Plan.Clear();

			for(var taskIndex = 0; taskIndex < Children.Count; taskIndex++)
			{
				// If the last plan is still running, we need to check whether the
				// new decomposition can possibly beat it.
				if (ctx.LastMTR != null && ctx.LastMTR.Count > 0)
				{
					// To reliably compare, we need the new decomposition record count to
					// at least be smaller than the last plan's record.
					if (ctx.MethodTraversalRecord.Count < ctx.LastMTR.Count)
					{
						// If the last plan's traversal record for this decomposition layer 
						// has a smaller index than the current task index we're about to
						// decompose, then the new decomposition can't possibly beat the
						// running plan, so we cancel finding a new plan.
						var currentDecompositionIndex = ctx.MethodTraversalRecord.Count;
						if (ctx.LastMTR[currentDecompositionIndex] < taskIndex)
						{
							ctx.MethodTraversalRecord.Add(-1);
							return null;
						}
					}
				}

				var task = Children[taskIndex];

				if ( task.IsValid( ctx ) == false )
					continue;

				if ( task is ICompoundTask compoundTask )
				{
					// We need to record the task index before we decompose the task,
					// so that the traversal record is set up in the right order.
					ctx.MethodTraversalRecord.Add(taskIndex);

					var result = compoundTask.Decompose( ctx );

					// If result is null, that means the entire planning procedure should cancel.
					if (result == null)
					{
						return null;
					}

					// If the decomposition failed
					if ( result.Count == 0 )
					{
						// Remove the taskIndex if it failed to decompose.
						ctx.MethodTraversalRecord.RemoveAt(ctx.MethodTraversalRecord.Count-1);
						continue;
					}

					int i = result.Count;
					while ( result.Count > 0 )
					{
						var res = result.Dequeue();
						Plan.Enqueue( res );
						i--;
						if (i < 0)
							break;
					}
				}
				else if( task is IPrimitiveTask primitiveTask )
				{
					primitiveTask.ApplyEffects( ctx );
					Plan.Enqueue( task );
				}

				// Break the moment we've selected a single sub-task that was successfully decomposed / validated.
				break;
			}

			return Plan;
		}
	}
}