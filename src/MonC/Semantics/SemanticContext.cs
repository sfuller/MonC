using System.Collections.Generic;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Expressions;

namespace MonC.Semantics
{
    public struct EnumDeclarationInfo
    {
        public EnumNode Enum;
        public EnumDeclarationNode Declaration;
        public int Value;
    }

    /// <summary>
    /// Contains semantic information shared between semantic modules.
    /// </summary>
    public class SemanticContext
    {
        public readonly Dictionary<string, FunctionDefinitionNode> Functions = new Dictionary<string, FunctionDefinitionNode>();
        public readonly Dictionary<string, EnumDeclarationInfo> EnumInfo = new Dictionary<string, EnumDeclarationInfo>();
        public readonly Dictionary<string, StructNode> Structs = new Dictionary<string, StructNode>();
        public readonly Dictionary<ISyntaxTreeNode, Symbol> SymbolMap = new Dictionary<ISyntaxTreeNode, Symbol>();
    }
}
