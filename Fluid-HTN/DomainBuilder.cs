using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.Operators;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN
{
    public class DomainBuilder< T > where T : IContext
    {
	    // ========================================================= FIELDS

	    protected readonly Domain< T > _domain;
		protected readonly List<ITask> _pointers = new List< ITask >();

	    // ========================================================= PROPERTIES

	    protected ITask Pointer
	    {
		    get
		    {
			    if ( _pointers.Count == 0 ) return null;
			    return _pointers[ _pointers.Count - 1 ];
		    }
	    }

	    // ========================================================= CONSTRUCTION

	    public DomainBuilder( string domainName )
	    {
			_domain = new Domain< T >( domainName );
			_pointers.Add( _domain.Root );
	    }

	    // ========================================================= HIERARCHY HANDLING

		public DomainBuilder<T> CompoundTask<P>( string name ) where P : ICompoundTask, new ()
		{
			if ( Pointer is ICompoundTask compoundTask )
			{
				var parent = new P() { Name = name };
				_domain.Add( compoundTask, parent );
				_pointers.Add( parent );
			}
			else
			{
				throw new Exception("Pointer is not a compound task type. Did you forget an End() after a Primitive Task Action was defined?");
			}

			return this;
		}

		public DomainBuilder<T> PrimitiveTask<P>( string name ) where P : IPrimitiveTask, new ()
		{
			if ( Pointer is ICompoundTask compoundTask )
			{
				var parent = new P() { Name = name };
				_domain.Add( compoundTask, parent );
				_pointers.Add( parent );
			}
			else
			{
				throw new Exception("Pointer is not a compound task type. Did you forget an End() after a Primitive Task Action was defined?");
			}

			return this;
		}

	    // ========================================================= COMPOUND TASKS

		/// <summary>
		/// A compound task that requires all child tasks to be valid.
		/// Child tasks can be sequences, selectors or actions.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Sequence( string name )
		{
			return CompoundTask< Sequence >( name );
	    }

		/// <summary>
		/// A compound task that requires a single child task to be valid.
		/// Child tasks can be sequences, selectors or actions.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Select( string name )
	    {
		    return CompoundTask< Selector >( name );
	    }

	    // ========================================================= PRIMITIVE TASKS

		/// <summary>
		/// A primitive task that can contain conditions, do-operators and effects.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Action( string name )
		{
			return PrimitiveTask< PrimitiveTask >( name );
		}

	    // ========================================================= CONDITIONS

		/// <summary>
		/// A precondition is a boolean statement required for the parent task to validate.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="condition"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Condition( string name, Func< T, bool > condition )
		{
			var cond = new FuncCondition<T>( name, condition );
			Pointer.AddCondition( cond );

		    return this;
	    }

	    // ========================================================= OPERATORS

		/// <summary>
		/// The operator of a primitive task, called Action.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Do( Func< T, TaskStatus > action )
	    {
		    if ( Pointer is IPrimitiveTask task )
		    {
			    var op = new FuncOperator< T >( action );
				task.SetOperator( op );
		    }
		    else
		    {
			    throw new Exception("Tried to add an Operator, but the Pointer is not a Primitive Task!");
		    }

		    return this;
	    }

	    // ========================================================= EFFECTS

		/// <summary>
		/// Effects can be added to a primitive task, called Action.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="effectType"></param>
		/// <param name="action"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Effect( string name, EffectType effectType, Action< T > action )
		{
			if ( Pointer is IPrimitiveTask task )
			{
				var effect = new ActionEffect< T >( name, effectType, action );
				task.AddEffect( effect );
			}
			else
			{
				throw new Exception("Tried to add an Effect, but the Pointer is not a Primitive Task!");
			}

			return this;
	    }

	    // ========================================================= OTHER OPERANDS

		/// <summary>
		/// Every task encapsulation must end with a call to End(), otherwise subsequent calls will be applied wrong.
		/// </summary>
		/// <returns></returns>
	    public DomainBuilder< T > End()
	    {
			_pointers.RemoveAt( _pointers.Count-1 );
		    return this;
	    }

		/// <summary>
		/// We can splice multiple domains together, allowing us to define reusable sub-domains.
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Splice( Domain<T> domain )
		{
			if ( Pointer is ICompoundTask compoundTask )
			{
				_domain.Add( compoundTask, domain.Root );
			}
			else
			{
				throw new Exception("Pointer is not a compound task type. Did you forget an End() after a Primitive Task Action was defined?");
			}

			return this;
		}

		/// <summary>
		/// Build the designed domain and return a domain instance.
		/// </summary>
		/// <returns></returns>
	    public Domain<T> Build()
	    {
		    return _domain;
	    }

		/// <summary>
		/// Builds the designed domain and saves it to a json file, then returns the domain instance.
		/// </summary>
		/// <param name="fileName"></param>
		public Domain<T> BuildAndSave( string fileName )
		{
			var domain = Build();
			domain.Save( fileName );
			return domain;
		}

		/// <summary>
		/// Loads a designed domain from a json file and returns a domain instance of it.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public Domain< T > Load( string fileName )
		{
			var domain = new Domain< T >(string.Empty);
			domain.Load( fileName );
			return domain;
		}
    }
}
