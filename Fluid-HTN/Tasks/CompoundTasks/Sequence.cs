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

		private readonly Queue< ITask > Plan = new Queue< ITask >();

		/// <summary>
		/// In a Sequence decomposition, all sub-tasks must be valid and successfully decomposed in order for the Sequence to be successfully decomposed.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		protected override Queue<ITask> OnDecompose( IContext ctx )
		{
			Plan.Clear();

			var oldCtx = ctx.Duplicate();

			foreach ( var task in Children )
			{
				if ( task.IsValid( ctx ) == false )
				{
					Plan.Clear();
					ctx.Copy( oldCtx );
					break;
				}

				if ( task is ICompoundTask compoundTask )
				{
					var result = compoundTask.Decompose( ctx );

					// If result is null, that means the entire planning procedure should cancel.
					if (result == null)
					{
						Plan.Clear();
						ctx.Copy( oldCtx );
						return null;
					}

					// If the decomposition failed
					if ( result.Count == 0 )
					{
						Plan.Clear();
						ctx.Copy( oldCtx );
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