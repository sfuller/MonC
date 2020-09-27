using MonC.SyntaxTree;

namespace MonC.TypeSystem.Types.Impl
{
    public class StructType : IValueType
    {
        private readonly StructNode _struct;

        public StructType(StructNode structNode)
        {
            _struct = structNode;
        }

        public string Name => _struct.Name;

        public string Represent()
        {
            return Name;
        }
    }
}
