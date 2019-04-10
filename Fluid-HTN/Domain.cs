using FluidHTN.Compounds;

namespace FluidHTN
{
	public class Domain<T> where T : IContext
	{
		// ========================================================= PROPERTIES

		public TaskRoot Root { get; }

		// ========================================================= CONSTRUCTION

		public Domain( string name )
		{
			Root = new TaskRoot() { Name = name, Parent = null };
		}

		// ========================================================= HIERARCHY HANDLING

		public void Add( ICompoundTask parent, ITask child )
		{
			parent.AddChild( child );
			child.Parent = parent;
		}
	}
}