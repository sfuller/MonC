namespace MonC.Bytecode
{
    public enum OpCode
    {
        NOOP,
        
        //
        // Accumulation
        //
        
        LOAD,
        LOADB,
        
        
        //
        // Stack
        //
        
        READ,
        WRITE,
        
        
        //
        // Calls
        //
        
        PUSHARG,
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
        NOT,
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