namespace MonC.IL
{
    public enum OpCode
    {
        NOOP,
        BREAK,

        //
        // Accumulation
        //

        //LOAD,


        //
        // Stack
        //

        READ,
        WRITE,
        PUSH,
        POP,


        //
        // Calls
        //

        CALL,
        RETURN,


        //
        // Comparison
        //

        CMPE,
        CMPLT,
        CMPLTE,

        //
        // Flow Control
        //

        JUMP,
        JUMPZ,
        JUMPNZ,

        //
        // Math
        //
        BOOL,
        LNOT,  // Logical not
        ADD,
        SUB,
        OR,
        AND,
        XOR,
        MUL,
        DIV,
        MOD
    }
}
