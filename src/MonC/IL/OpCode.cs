namespace MonC.IL
{
    public enum OpCode
    {
        /// <summary>
        /// No Operation.
        /// </summary>
        NOOP,

        /// <summary>
        /// Signal a break.
        /// </summary>
        BREAK,


        //
        // Stack
        //

        /// <summary>
        /// Read bytes from an absolute location in the stack and push them to the top of the stack.
        /// Immediate Value: Absolute location to read from, Size Value: Number of bytes to read and push.
        /// </summary>
        READ,

        /// <summary>
        /// Write bytes from the top of the stack to an absolute location in the stack. The bytes at the top of the
        /// stack are not popped.
        /// Immeidate Value: Absolute location to write to, Size Value: Number of bytes to read and write.
        /// </summary>
        WRITE,

        /// <summary>
        /// Push the immediate value to the top of the stack.
        /// Immediate Value: Word value to push.
        /// </summary>
        PUSHWORD,

        // /// <summary>
        // /// Push data pointed to by the immediate value to the top of the stack.
        // /// Immediate Value: Data entry index of the data push.
        // /// </summary>
        //PUSHDATA, -- soon

        /// <summary>
        /// Increment the stack pointer by the size value.
        /// Size Value: Number of bytes to increment the stack pointer by.
        /// </summary>
        PUSH,

        /// <summary>
        /// Decrement the stack pointer by the size value.
        /// Size Value: Number of bytes to decrement the stack pointer by.
        /// </summary>
        POP,


        /// <summary>
        /// Pops Size bytes off the stack, and pushes the bytes that occured after the offset specified by the immediate
        /// value.
        /// </summary>
        ACCESS,

        /// <summary>
        /// Pushes the native pointer to the element on the stack pointed to by the immediate value.
        /// </summary>
        ADDRESSOF,

        /// <summary>
        /// Pops a native pointer off the stack and reads size bytes from the memory pointed to. These read bytes are
        /// pushed onto the stack.
        /// </summary>
        DEREF,


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
