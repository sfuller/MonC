namespace MonC.Bytecode
{
    public enum Opcode
    {
        NOOP,
        
        //
        // Stack
        //
        
        /// <summary>
        /// Pushes the immediate value onto the stack.
        /// </summary>
        PUSH_IMMEDIATE,
        
        /// <summary>
        /// Pushes the pointer to the data based on the data offset specified by the immediate value.
        /// </summary>
        PUSH_DATA,
        
        
        //
        // Math
        //
        
        /// <summary>
        /// Pops a value off of the stack and pushes the logical not of the popped value onto the stack.
        /// </summary>
        NOT,
        
        /// <summary>
        /// Pops two values off of the stack and adds them together. The result is pushed onto the stack. 
        /// </summary>
        ADD,
        
        /// <summary>
        /// Pops two values off of the stack. The first value popped is subtracted from the second value popped and the
        /// result is pushed onto the stack.
        /// </summary>
        SUB,
        
        
        //
        // Comparison
        //
        
        /// <summary>
        /// Pops two values off of the stack. If the two values are equal, 1 is pushed onto the stack. Otherwise, 0 is
        /// pushed onto the stack.
        /// </summary>
        COMPARE_EQUAL,
        
        /// <summary>
        /// Pops two values off of the stack. If the first popped value is less than the second value popped, 1 is
        /// pushed onto the stack. Otherwise, 0 is pushed onto the stack.
        /// </summary>
        COMPARE_LT,
        
        /// <summary>
        /// Pops two values off of the stack. If the first popped value is less than or equal the second value popped,
        /// 1 is pushed onto the stack. Otherwise, 0 is pushed onto the stack.
        /// </summary>
        COMPARE_LTE.
        
        
        //
        // Flow control
        //
        
        /// <summary>
        /// Pops a value and jumps to the instruction relative to the immediate value if the popped value is not zero. 
        /// </summary>
        BRANCH,
        
        /// <summary>
        /// Pops a value and calls the value as a function.
        /// </summary>
        CALL
        
    }
}