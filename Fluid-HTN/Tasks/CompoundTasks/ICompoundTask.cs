using System.Collections.Generic;

namespace FluidHTN.Compounds
{
	public interface ICompoundTask : ITask
	{
		List<ITask> Children { get; }
		ICompoundTask AddChild( ITask child );

		/// <summary>
		/// Decompose the task onto the tasks to process queue, mind it's depth first
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		Queue<ITask> Decompose(IContext ctx);
	}
}