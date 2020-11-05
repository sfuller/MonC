using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonC.SyntaxTree;
using MonC.SyntaxTree.Nodes;
using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Specifiers;
using MonC.SyntaxTree.Nodes.Statements;
using MonC.SyntaxTree.Util;
using MonC.SyntaxTree.Util.ChildrenVisitors;
using MonC.SyntaxTree.Util.Delegators;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Semantics
{
    public class LValuesAssignedBeforeUseValidator : IVisitor<DeclarationNode>
    {
        private readonly List<LValue> _lvalues = new List<LValue>();
        private readonly LValueAssignmentValidator _lValueAssignmentValidator;

        public LValuesAssignedBeforeUseValidator(IErrorManager errors)
        {
            _lValueAssignmentValidator = new LValueAssignmentValidator(errors);
        }

        public void Process(FunctionDefinitionNode function)
        {
            SyntaxTreeDelegator visitor = new SyntaxTreeDelegator();
            StatementDelegator statementDelegator = new StatementDelegator();
            visitor.StatementVisitor = statementDelegator;
            statementDelegator.DeclarationVisitor = this;

            SyntaxTreeDelegator childrenVisitor = new SyntaxTreeDelegator();
            StatementChildrenVisitor statementChildrenVisitor = new StatementChildrenVisitor(visitor, childrenVisitor);
            childrenVisitor.StatementVisitor = statementChildrenVisitor;

            // Populate lvalue list
            function.Body.AcceptStatementVisitor(statementChildrenVisitor);

            // Validate usage of all lvalues.
            foreach (LValue lvalue in _lvalues) {
                _lValueAssignmentValidator.Validate(lvalue, function);
            }
        }

        public void Visit(DeclarationNode node)
        {
            CreateLValuesForDeclaration(new List<DeclarationNode>(), node);
        }

        private void CreateLValuesForDeclaration(List<DeclarationNode> root, DeclarationNode delcaration)
        {
            List<DeclarationNode> path = new List<DeclarationNode>(root.Count + 1);
            path.AddRange(root);
            path.Add(delcaration);

            if (((TypeSpecifierNode) delcaration.Type).Type is StructType structType) {
                CreateLValuesForStruct(root, structType.Struct);
            } else {
                // Only wory about this lvalue if it's not assigned inline.
                // TODO: Can structs member declarations have default values? Not sure if this is implemented..

                // TODO: Better way of signifying if assignment is present?
                // Maybe assignment should be a separate node in the AST?
                if (delcaration.Assignment is VoidExpressionNode) {
                    _lvalues.Add(new LValue(path));
                }
            }
        }

        private void CreateLValuesForStruct(List<DeclarationNode> root, StructNode structNode)
        {
            foreach (DeclarationNode member in structNode.Members) {
                CreateLValuesForDeclaration(root, member);
            }
        }


    }
}
