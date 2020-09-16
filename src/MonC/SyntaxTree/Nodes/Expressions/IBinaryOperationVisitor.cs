using MonC.SyntaxTree.Nodes.Expressions.BinaryOperations;

namespace MonC.SyntaxTree.Nodes.Expressions
{
    public interface IBinaryOperationVisitor
    {
        void VisitCompareLTBinOp(CompareLtBinOpNode node);
        void VisitCompareLTEBinOp(CompareLteBinOpNode node);
        void VisitCompareGTBinOp(CompareGtBinOpNode node);
        void VisitCompareGTEBinOp(CompareGteBinOpNode node);
        void VisitCompareEqualityBinOp(CompareEqualityBinOpNode node);
        void VisitCompareInequalityBinOp(CompareInequalityBinOpNode node);
        void VisitLogicalAndBinOp(LogicalAndBinOpNode node);
        void VisitLogicalOrBinOp(LogicalOrBinOpNode node);
        void VisitAddBinOp(AddBinOpNode node);
        void VisitSubtractBinOp(SubtractBinOpNode node);
        void VisitMultiplyBinOp(MultiplyBinOpNode node);
        void VisitDivideBinOp(DivideBinOpNode node);
        void VisitModuloBinOp(ModuloBinOpNode node);

        void VisitUnknown(IBinaryOperationNode node);
    }
}
