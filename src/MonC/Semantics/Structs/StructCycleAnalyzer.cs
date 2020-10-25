using System.Collections.Generic;
using System.Text;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Semantics.Structs
{
    public class StructCycleAnalyzer
    {
        private readonly HashSet<StructNode> _analyzedStructs = new HashSet<StructNode>();

        private readonly Stack<(StructNode node, int declIndex)> _structStack
            = new Stack<(StructNode node, int declIndex)>();

        public void Analyze(StructNode structNode, IErrorManager errorManager)
        {
            _analyzedStructs.Clear();
            _structStack.Clear();

            _structStack.Push((structNode, 0));

            while (_structStack.Count > 0) {
                (StructNode currentStruct, int declIndex) = _structStack.Pop();

                for (int i = declIndex, ilen = currentStruct.Members.Count; i < ilen; ++i) {
                    DeclarationNode nextDecl = currentStruct.Members[i];

                    IType type = ((TypeSpecifierNode) nextDecl.Type).Type;
                    if (!(type is StructType structType)) {
                        continue;
                    }

                    StructNode declStruct = structType.Struct;

                    if (_analyzedStructs.Contains(declStruct)) {
                        continue;
                    }

                    _structStack.Push((currentStruct, i + 1));

                    if (declStruct == structNode) {
                        AddError(structNode, errorManager);
                    }

                    _analyzedStructs.Add(declStruct);
                    _structStack.Push((declStruct, 0));
                }
            }

        }

        private void AddError(StructNode structBeingAnalyzed, IErrorManager errorManager)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Cycle detected in struct declarations for struct {structBeingAnalyzed.Name}. Chain:");
            foreach ((StructNode structNode, int declIndex) in _structStack) {
                builder.AppendLine($"  Struct {structNode.Name}, member {structNode.Members[declIndex - 1].Name}");
            }

            errorManager.AddError(builder.ToString(), structBeingAnalyzed);
        }

    }
}
