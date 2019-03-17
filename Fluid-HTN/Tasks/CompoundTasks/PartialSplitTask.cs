using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;

namespace FluidHTN
{
	public class PartialSplitTask : ITask
	{
		public int DomainIndex { get; }

		public string Name { get; set; }

		public ICompoundTask Parent { get; set; }

		public List< ICondition > Conditions { get; } = null;
		public ITask AddCondition( ICondition condition )
		{
			throw new Exception("Partial Split tasks does not support conditions.");
		}

		public TaskStatus LastStatus { get; }
		public bool IsValid( IContext ctx )
		{
			return true;
		}

		public List< IEffect > Effects { get; } = null;

		public ITask AddEffect( IEffect effect )
		{
			throw new Exception("Partial Split tasks does not support effects.");
		}

		public void ApplyEffects( IContext ctx )
		{
		}
	}
}