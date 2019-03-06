using System;

namespace FluidHTN.Operators
{
	public class FuncOperator<T> : IOperator where T : IContext
	{
		private readonly Func< T, TaskStatus > _func;

		public FuncOperator( Func< T, TaskStatus > func )
		{
			_func = func;
		}

		public TaskStatus Update( IContext ctx )
		{
			if ( ctx is T c )
			{
				return _func?.Invoke( c ) ?? TaskStatus.Failure;
			}
			else
			{
				throw new Exception("Unexpected context type!");
			}
		}
	}
}