namespace MonC.Codegen
{
    public interface IVariable
    {
        void Accept(IVariableVisitor visitor);
    }
}