namespace FluidHTN.Compounds
{
    /// <summary>
    ///     The Decompose All interface is a tag to signify that this compound task type intends to
    ///     decompose all its children.
    ///     For a task to support Partial Split tasks, needed for partial planning, it must be
    ///     a decompose-all compound task type.
    /// </summary>
    public interface IDecomposeAll : ICompoundTask
    {
    }
}