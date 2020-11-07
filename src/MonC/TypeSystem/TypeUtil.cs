using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.TypeSystem.Types;

namespace MonC.TypeSystem
{
    public static class TypeUtil
    {
        public static bool IsVoid(ITypeSpecifierNode specifier)
        {
            if (specifier is TypeSpecifierNode parsedSpecifier) {
                return IsVoid(parsedSpecifier.Type);
            }
            return false;
        }

        public static bool IsVoid(IType type)
        {
            if (type is IPrimitiveType primitiveType) {
                return primitiveType.Primitive == Primitive.Void;
            }
            return false;
        }

        public static IType? GetTypeFromSpecifier(ITypeSpecifierNode specifierNode)
        {
            if (specifierNode is TypeSpecifierNode parsedSpecifier) {
                return parsedSpecifier.Type;
            }
            return null;
        }
    }
}
