using System;

namespace MonC.SyntaxTree
{
    public enum PointerType
    {
        NotAPointer,
        Shared,
        Weak,
        Owned,
        Borrowed
    }
}