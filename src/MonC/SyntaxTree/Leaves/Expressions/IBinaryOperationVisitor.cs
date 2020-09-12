using MonC.SyntaxTree.Leaves.Expressions.BinaryOperations;

namespace MonC.SyntaxTree.Leaves.Expressions
{
    public interface IBinaryOperationVisitor
    {
        void VisitCompareLTBinOp(CompareLTBinOpLeaf leaf);
        void VisitCompareLTEBinOp(CompareLTEBinOpLeaf leaf);
        void VisitCompareGTBinOp(CompareGTBinOpLeaf leaf);
        void VisitCompareGTEBinOp(CompareGTEBinOpLeaf leaf);
        void VisitCompareEqualityBinOp(CompareEqualityBinOpLeaf leaf);
        void VisitCompareInequalityBinOp(CompareInequalityBinOpLeaf leaf);
        void VisitLogicalAndBinOp(LogicalAndBinOpLeaf leaf);
        void VisitLogicalOrBinOp(LogicalOrBinOpLeaf leaf);
        void VisitAddBinOp(AddBinOpLeaf leaf);
        void VisitSubtractBinOp(SubtractBinOpLeaf leaf);
        void VisitMultiplyBinOp(MultiplyBinOpLeaf leaf);
        void VisitDivideBinOp(DivideBinOpLeaf leaf);
        void VisitModuloBinOp(ModuloBinOpLeaf leaf);

        void VisitUnknown(IBinaryOperationLeaf leaf);
    }
}
