namespace MonC.Bytecode
{
    public enum OpCode
    {
        NOOP,
        BREAK,
        
        //
        // Accumulation
        //
        
        LOAD,


        //
        // Stack
        //
        
        READ,
        WRITE,
        
        
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