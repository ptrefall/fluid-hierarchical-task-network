using System.Collections.Generic;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
	public class Sequence : CompoundTask
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
		/// In a Sequence decomposition, all sub-tasks must be valid and successfully decomposed in order for the Sequence to be successfully decomposed.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		protected override Queue<ITask> OnDecompose( IContext ctx )
		{
			Plan.Clear();

			foreach ( var task in Children )
			{
				if ( task.IsValid( ctx ) == false )
				{
					//TODO: Remove effects
					Plan.Clear();
					break;
				}

				if ( task is ICompoundTask compoundTask )
				{
					var result = compoundTask.Decompose( ctx );

					// If the decomposition failed
					if ( result.Count == 0 )
					{
						//TODO: Remove effects
						Plan.Clear();
						break;
					}

					while ( result.Count > 0 )
					{
						Plan.Enqueue( result.Dequeue() );
					}
				}
				else if ( task is IPrimitiveTask primitiveTask )
				{
					primitiveTask.ApplyEffects( ctx );
					Plan.Enqueue( task );
				}
			}

			return Plan;
		}
	}
}