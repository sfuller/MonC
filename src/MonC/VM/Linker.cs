using System.Collections.Generic;
using System.Linq;
using MonC.Bytecode;
using MonC.Codegen;

namespace MonC.VM
{
    public class Linker
    {
        private readonly List<ILModule> _inputModules = new List<ILModule>();
        private readonly List<KeyValuePair<string, VMEnumerable>> _boundFunctions = new List<KeyValuePair<string, VMEnumerable>>();
        
        private readonly Dictionary<string, int> _exportedFunctionIndices = new Dictionary<string, int>();
        private readonly List<ILFunction> _functionImplementations = new List<ILFunction>();
        private readonly List<int> _moduleOffsets = new List<int>();

        private readonly List<string> _strings = new List<string>();

        private readonly List<LinkError> _errors;

        public Linker(List<LinkError> errors)
        {
            _errors = errors;
        }

        public void AddModule(ILModule module)
        {
            _inputModules.Add(module);
        }

        public void AddFunctionBinding(string name, VMFunction function)
        {
            AddFunctionBinding(name, new VMEnumerableWrapper(function).MakeEnumerator);
        }

        public void AddFunctionBinding(string name, VMEnumerable enumerable)
        {
            _boundFunctions.Add(new KeyValuePair<string, VMEnumerable>(name, enumerable));
        }

        public VMModule Link()
        {
            _exportedFunctionIndices.Clear();
            _functionImplementations.Clear();
            _moduleOffsets.Clear();

            foreach (ILModule module in _inputModules) {
                ImportModule(module);    
            }
            
            ImportBoundFunctions();

            for (int i = 0, ilen = _inputModules.Count; i < ilen; ++i) {
                LinkModule(i);
            }

            ILModule resultModule = new ILModule {
                DefinedFunctions = _functionImplementations.ToArray(),
                ExportedFunctions = _exportedFunctionIndices.ToArray(),
                UndefinedFunctionNames = new string[0],
                Strings = _strings.ToArray()
            };

            int vmFunctionsOffset = _functionImplementations.Count;
            Dictionary<int, VMEnumerable> vmFunctions = new Dictionary<int, VMEnumerable>(_boundFunctions.Count);
            for (int i = 0, ilen = _boundFunctions.Count; i < ilen; ++i) {
                vmFunctions.Add(vmFunctionsOffset + i, _boundFunctions[i].Value);    
            }

            return new VMModule(resultModule, vmFunctions);
        }

        public void ImportModule(ILModule inputModule)
        {
            int baseIndex = _functionImplementations.Count;
            _moduleOffsets.Add(baseIndex);

            // Make copies of implementations for modification and do some modifications while we're currently
            // iterating over all of the functions.
            for (int i = 0, ilen = inputModule.DefinedFunctions.Length; i < ilen; ++i) {
                ILFunction function = inputModule.DefinedFunctions[i];
                function.Code = function.Code.ToArray();
                _functionImplementations.Add(function);
                
                RelocateStrings(function, _strings.Count);
            }
            
            foreach (KeyValuePair<string, int> exportedFunction in inputModule.ExportedFunctions) {
                if (_exportedFunctionIndices.ContainsKey(exportedFunction.Key)) {
                    _errors.Add(new LinkError {Message = $"Conflicting exported function {exportedFunction.Key}"});
                    continue;
                }
                _exportedFunctionIndices[exportedFunction.Key] = baseIndex + exportedFunction.Value;
            }
            
            // Import all of the strings (instructions referencing string indices were modified above)
            _strings.AddRange(inputModule.Strings);
        }

        private void RelocateStrings(ILFunction function, int newBaseOffset)
        {
            foreach (int index in function.StringInstructions) {
                Instruction ins = function.Code[index];
                ins.ImmediateValue += newBaseOffset;
                function.Code[index] = ins;
            }
        }

        private void ImportBoundFunctions()
        {
            int baseIndex = _functionImplementations.Count;
            
            for (int i = 0, ilen = _boundFunctions.Count; i < ilen; ++i) {
                KeyValuePair<string, VMEnumerable> vmFunction = _boundFunctions[i];
                if (_exportedFunctionIndices.ContainsKey(vmFunction.Key)) {
                    _errors.Add(new LinkError {Message = $"Conflicting bound function {vmFunction.Key}"});
                    continue;
                }
                _exportedFunctionIndices[vmFunction.Key] = baseIndex + i;
            }
        }

        private void LinkModule(int index)
        {
            ILModule module = _inputModules[index];
            int baseIndex = _moduleOffsets[index];

            for (int i = 0, ilen = module.DefinedFunctions.Length; i < ilen; ++i) {
                Instruction[] newImpl = _functionImplementations[baseIndex + i].Code;
                LinkFunction(baseIndex, module, newImpl);
            }
        }

        private void LinkFunction(int baseIndex, ILModule module, Instruction[] impl)
        {
            int definedCount = module.DefinedFunctions.Length;

            for (int i = 0, ilen = impl.Length; i < ilen; ++i) {
                Instruction ins = impl[i];
                if (ins.Op != OpCode.CALL) {
                    continue;
                }

                if (ins.ImmediateValue < definedCount) {
                    // This is a call to a module local function that just needs to be offset
                    ins.ImmediateValue += baseIndex;
                } else {
                    // This is a function to an exported function defined outside of the module.
                    string functionName = module.UndefinedFunctionNames[ins.ImmediateValue - definedCount];

                    if (!_exportedFunctionIndices.TryGetValue(functionName, out ins.ImmediateValue)) {
                        _errors.Add(new LinkError {Message = $"Undefined function {functionName}"});
                    }
                }

                impl[i] = ins;
            }
            
        }

    }
}