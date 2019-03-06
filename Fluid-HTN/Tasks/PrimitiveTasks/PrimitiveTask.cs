using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Operators;

namespace FluidHTN.PrimitiveTasks
{
	public class PrimitiveTask : IPrimitiveTask
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

		public TaskStatus LastStatus { get; }
		public bool IsValid( IContext ctx )
		{
			foreach ( var condition in Conditions )
			{
				if ( condition.IsValid( ctx ) == false )
					return false;
			}

			return true;
		}

		public IOperator Operator { get; private set; }
		public void SetOperator( IOperator action )
		{
			if ( Operator != null )
			{
				throw new Exception("A Primitive Task can only contain a single Operator!");
			}

			Operator = action;
		}

		public List< IEffect > Effects { get; } = new List< IEffect >();

		public ITask AddEffect( IEffect effect )
		{
			Effects.Add( effect );
			return this;
		}

		public void ApplyEffects( IContext ctx )
		{
			foreach ( var effect in Effects )
			{
				effect.Apply( ctx );
			}
		}
	}
}