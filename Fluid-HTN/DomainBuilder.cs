using System;

namespace FluidHTN
{
    public class DomainBuilder< T > where T : IContext
    {
	    // ========================================================= COMPOSITE TASKS

		/// <summary>
		/// A composite task that requires all child tasks to be valid.
		/// Child tasks can be sequences, selectors or actions.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Sequence( string name )
	    {
		    return this;
	    }

		/// <summary>
		/// A composite task that requires one child task to be valid.
		/// Child tasks can be sequences, selectors or actions.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Select( string name )
	    {
		    return this;
	    }

	    // ========================================================= PRIMITIVE TASKS

		/// <summary>
		/// A primitive task that can contain conditions, do-operators and effects.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Action( string name )
	    {
		    return this;
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
		    return this;
	    }

	    // ========================================================= OTHER OPERANDS

		/// <summary>
		/// Every task encapsulation must end with a call to End(), otherwise the Build won't produce the intended Domain.
		/// </summary>
		/// <returns></returns>
	    public DomainBuilder< T > End()
	    {
		    return this;
	    }

		/// <summary>
		/// We can splice multiple domains together, allowing us to define reusable sub-domains.
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
	    public DomainBuilder< T > Splice( Domain<T> domain )
	    {
		    return this;
	    }

		/// <summary>
		/// Build the designed domain and return a domain instance.
		/// </summary>
		/// <returns></returns>
	    public Domain<T> Build()
	    {
		    return new Domain<T>();
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
			var domain = new Domain< T >();
			domain.Load( fileName );
			return domain;
		}
    }
}
