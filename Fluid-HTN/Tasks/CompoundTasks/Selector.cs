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

		private static readonly Queue< ITask > Plan = new Queue< ITask >();

		/// <summary>
		/// In a Selector decomposition, just a single sub-task must be valid and successfully decompose for the Selector to be successfully decomposed.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		protected override Queue<ITask> OnDecompose( IContext ctx )
		{
			Plan.Clear();

			foreach ( var task in Children )
			{
				if ( task.IsValid( ctx ) == false )
					continue;

				if ( task is ICompoundTask compoundTask )
				{
					var result = compoundTask.Decompose( ctx );

					// If the decomposition failed
					if ( result.Count == 0 )
					{
						continue;
					}

					while ( result.Count > 0 )
					{
						Plan.Enqueue( result.Dequeue() );
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