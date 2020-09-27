using MonC.SyntaxTree.Nodes.Expressions;
using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;

namespace MonC.SyntaxTree.Util.Delegators
{
    public class BinaryOperationDelegator : IBinaryOperationVisitor
    {
        public IVisitor<CompareLtBinOpNode>? CompareLtBinOpVisitor;
        public IVisitor<CompareLteBinOpNode>? CompareLteBinOpVisitor;
        public IVisitor<CompareGtBinOpNode>? CompareGtBinOpVisitor;
        public IVisitor<CompareGteBinOpNode>? CompareGteBinOpNode;
        public IVisitor<CompareEqualityBinOpNode>? CompareEqualityBinOpVisitor;
        public IVisitor<CompareInequalityBinOpNode>? CompareInequalityBinOpVisitor;
        public IVisitor<LogicalAndBinOpNode>? LogicalAndBinOpVisitor;
        public IVisitor<LogicalOrBinOpNode>? LogicalOrBinOpVisitor;
        public IVisitor<AddBinOpNode>? AddBinOpVisitor;
        public IVisitor<SubtractBinOpNode>? SubtractBinOpVisitor;
        public IVisitor<MultiplyBinOpNode>? MultiplyBinOpVisitor;
        public IVisitor<DivideBinOpNode>? DivideBinOpVisitor;
        public IVisitor<ModuloBinOpNode>? ModuloBinOpVisitor;
        public IVisitor<IBinaryOperationNode>? UnknownVisitor;

        public void VisitCompareLTBinOp(CompareLtBinOpNode node)
        {
            CompareLtBinOpVisitor?.Visit(node);
        }

        public void VisitCompareLTEBinOp(CompareLteBinOpNode node)
        {
            CompareLteBinOpVisitor?.Visit(node);
        }

        public void VisitCompareGTBinOp(CompareGtBinOpNode node)
        {
            CompareGtBinOpVisitor?.Visit(node);
        }

        public void VisitCompareGTEBinOp(CompareGteBinOpNode node)
        {
            CompareGteBinOpNode?.Visit(node);
        }

        public void VisitCompareEqualityBinOp(CompareEqualityBinOpNode node)
        {
            CompareEqualityBinOpVisitor?.Visit(node);
        }

        public void VisitCompareInequalityBinOp(CompareInequalityBinOpNode node)
        {
            CompareInequalityBinOpVisitor?.Visit(node);
        }

        public void VisitLogicalAndBinOp(LogicalAndBinOpNode node)
        {
            LogicalAndBinOpVisitor?.Visit(node);
        }

        public void VisitLogicalOrBinOp(LogicalOrBinOpNode node)
        {
            LogicalOrBinOpVisitor?.Visit(node);
        }

        public void VisitAddBinOp(AddBinOpNode node)
        {
            AddBinOpVisitor?.Visit(node);
        }

        public void VisitSubtractBinOp(SubtractBinOpNode node)
        {
            SubtractBinOpVisitor?.Visit(node);
        }

        public void VisitMultiplyBinOp(MultiplyBinOpNode node)
        {
            MultiplyBinOpVisitor?.Visit(node);
        }

        public void VisitDivideBinOp(DivideBinOpNode node)
        {
            DivideBinOpVisitor?.Visit(node);
        }

        public void VisitModuloBinOp(ModuloBinOpNode node)
        {
            ModuloBinOpVisitor?.Visit(node);
        }

        public void VisitUnknown(IBinaryOperationNode node)
        {
            UnknownVisitor?.Visit(node);
        }
    }
}
