namespace FluidHTN
{
	public interface IContext
	{
		/// <summary>
		/// Duplicate only the world state that will have the potential to change through effects during planning.
		/// </summary>
		/// <returns></returns>
		IContext Duplicate();

		/// <summary>
		/// Copies the values of ctx that has the potential to change through effects during planning.
		/// </summary>
		/// <param name="ctx"></param>
		void Copy( IContext ctx );
	}
}