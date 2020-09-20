namespace MonC.TypeSystem.Types
{
    public interface IType
    {
        /// <summary>
        /// Create a string representation as it would appear in MonC source code.
        /// </summary>
        // NOTE: If we wish to decouple this knowledge from the type objects, we could make types support the visitor
        // pattern, and have a visitor implementation that generates representations. For now I have decided to keep it
        // simple. I don't need if we need such separation yet.
        string Represent();
    }
}
