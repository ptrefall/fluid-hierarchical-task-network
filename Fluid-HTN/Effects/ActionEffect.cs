using System;

namespace FluidHTN.Effects
{
	public class ActionEffect<T> : IEffect where T : IContext
	{
		public string Name { get; }
		public EffectType Type { get; }
		private readonly Action< T > _action;

		public ActionEffect( string name, EffectType type, Action< T > action )
		{
			Name = name;
			Type = type;
			_action = action;
		}

		public void Apply( IContext ctx )
		{
			if ( ctx is T c )
			{
				_action?.Invoke( c );
			}
			else
			{
				throw new Exception("Unexpected context type!");
			}
		}
	}
}