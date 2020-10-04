using MonC.SyntaxTree;

namespace MonC.TypeSystem.Types.Impl
{
    public class StructType : IValueType
    {
        public readonly StructNode Struct;

        public StructType(StructNode structNode)
        {
            Struct = structNode;
        }

        public string Name => Struct.Name;

        public string Represent()
        {
            return Name;
        }
    }
}
