using System.Collections.Generic;
using FluidHTN.Conditions;

namespace FluidHTN.Compounds
{
	public abstract class CompoundTask : ICompoundTask

	{
		public int DomainIndex { get; }

		public string Name { get; set; }

		public ICompoundTask Parent { get; set; }

		public List< ICondition > Conditions { get; } = new List< ICondition >();

		public ITask AddCondition( ICondition condition )
		{
			Conditions.Add( condition );
			return this;
		}

		public TaskStatus LastStatus { get; private set; }

		public Queue<ITask> Decompose( IContext ctx, int startIndex )
		{
			return OnDecompose(ctx, startIndex);
		}

		protected abstract Queue<ITask> OnDecompose(IContext ctx, int startIndex);

		public List< ITask > Children { get; } = new List< ITask >();
		public ICompoundTask AddChild( ITask child )
		{
			Children.Add( child );
			return this;
		}

		public virtual bool IsValid( IContext ctx )
		{
			foreach ( var condition in Conditions )
			{
				if ( condition.IsValid( ctx ) == false )
					return false;
			}

			return true;
		}
	}
}