using System.Collections.Generic;

namespace MonC.VM
{
    public enum ContinuationAction
    {
        RETURN,
        CALL,
        YIELD,
        UNWRAP
    }
    
    public struct Continuation
    {
        public ContinuationAction Action;
        public int ReturnValue;
        public int FunctionIndex;
        public VMModule Module;
        public IReadOnlyList<int> Arguments;
        public IYieldToken YieldToken;
        public IEnumerator<Continuation> ToUnwrap;

        public static Continuation Return(int returnValue)
        {
            return new Continuation {
                Action = ContinuationAction.RETURN,
                ReturnValue = returnValue
            };
        }

        public static Continuation Call(VMModule module, int functionIndex, IReadOnlyList<int> arguments)
        {
            return new Continuation {
                Action = ContinuationAction.CALL,
                Module = module,
                FunctionIndex = functionIndex,
                Arguments = arguments
            };
        }

        public static Continuation Call(VMModule module, string functionName, IReadOnlyList<int> arguments)
        {
            int index = VirtualMachine.LookupFunction(module.ILModule, functionName);
            return Call(module, index, arguments);
        }

        public static Continuation Yield(IYieldToken token)
        {
            return new Continuation {
                Action = ContinuationAction.YIELD,
                YieldToken = token
            };
        }

        public static Continuation Unwrap(IEnumerator<Continuation> toUnwrap)
        {
            return new Continuation {
                Action = ContinuationAction.UNWRAP,
                ToUnwrap = toUnwrap
            };
        }
        
    }
}