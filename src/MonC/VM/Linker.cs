using System.Collections.Generic;
using System.Linq;
using MonC.IL;

namespace MonC.VM
{
    public class Linker
    {
        private struct InputModule
        {
            public ILModule Module;
            public bool Exported;
        }

        private struct BoundFunction
        {
            public string Name;
            public VMFunction Binding;
            public bool Exported;
        }

        private readonly List<InputModule> _inputModules = new List<InputModule>();
        private readonly List<BoundFunction> _boundFunctions = new List<BoundFunction>();

        private readonly Dictionary<string, int> _functionIndicesByName = new Dictionary<string, int>();
        private readonly List<ILFunction> _functionImplementations = new List<ILFunction>();
        private readonly List<int> _moduleOffsets = new List<int>();
        private readonly List<KeyValuePair<string, int>> _exportedFunctions = new List<KeyValuePair<string, int>>();

        private readonly List<string> _strings = new List<string>();
        private readonly Dictionary<string, int> _undefinedFunctionsIndicesByName = new Dictionary<string, int>();

        private readonly List<LinkError> _errors;

        public Linker(List<LinkError> errors)
        {
            _errors = errors;
        }

        public void AddModule(ILModule module, bool export)
        {
            _inputModules.Add(new InputModule { Module = module, Exported = export });
        }

        public void AddFunctionBinding(string name, VMFunction enumerable, bool export)
        {
            _boundFunctions.Add(new BoundFunction { Name = name, Binding = enumerable, Exported = export});
        }

        public VMModule Link(bool allowUndefinedReferences = false)
        {
            _functionIndicesByName.Clear();
            _functionImplementations.Clear();
            _moduleOffsets.Clear();

            foreach (InputModule module in _inputModules) {
                ImportModule(module);
            }

            ImportBoundFunctions();

            for (int i = 0, ilen = _inputModules.Count; i < ilen; ++i) {
                LinkModule(i);
            }

            if (!allowUndefinedReferences) {
                foreach (string functionName in _undefinedFunctionsIndicesByName.Keys) {
                    _errors.Add(new LinkError {Message = $"Undefined function {functionName}"});
                }
            }

            ILModule resultModule = new ILModule {
                DefinedFunctions = _functionImplementations.ToArray(),
                ExportedFunctions = _exportedFunctions.ToArray(),
                ExportedEnumValues = _inputModules
                        .Where(m => m.Exported)
                        .Select(m => m.Module)
                        .SelectMany(m => m.ExportedEnumValues)
                        .ToArray(),
                UndefinedFunctionNames = _undefinedFunctionsIndicesByName
                        .OrderBy(k => k.Value)
                        .Select(k => k.Key).ToArray(),
                Strings = _strings.ToArray()
            };

            int vmFunctionsOffset = _functionImplementations.Count;
            Dictionary<int, VMFunction> vmFunctions = new Dictionary<int, VMFunction>(_boundFunctions.Count);
            for (int i = 0, ilen = _boundFunctions.Count; i < ilen; ++i) {
                vmFunctions.Add(vmFunctionsOffset + i, _boundFunctions[i].Binding);
            }

            return new VMModule(resultModule, vmFunctions);
        }

        private void ImportModule(InputModule inputModule)
        {
            int baseIndex = _functionImplementations.Count;
            _moduleOffsets.Add(baseIndex);

            // Make copies of implementations for modification and do some modifications while we're currently
            // iterating over all of the functions.

            foreach (ILFunction function in inputModule.Module.DefinedFunctions) {
                ILFunction newFunction = function;
                newFunction.Code = function.Code.ToArray();
                _functionImplementations.Add(newFunction);
                RelocateStrings(function, _strings.Count);
            }

            foreach (KeyValuePair<string, int> exportedFunction in inputModule.Module.ExportedFunctions) {
                if (_functionIndicesByName.ContainsKey(exportedFunction.Key)) {
                    _errors.Add(new LinkError {Message = $"Conflicting exported function {exportedFunction.Key}"});
                    continue;
                }
                int index = baseIndex + exportedFunction.Value;
                _functionIndicesByName[exportedFunction.Key] = index;
                if (inputModule.Exported) {
                    _exportedFunctions.Add(exportedFunction);
                }
            }

            // Import all of the strings (instructions referencing string indices were modified above)
            _strings.AddRange(inputModule.Module.Strings);
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

                BoundFunction function = _boundFunctions[i];
                if (_functionIndicesByName.ContainsKey(function.Name)) {
                    _errors.Add(new LinkError {Message = $"Conflicting bound function {function.Name}"});
                    continue;
                }
                int index = baseIndex + i;
                _functionIndicesByName[function.Name] = index;
                if (function.Exported) {
                    _exportedFunctions.Add(new KeyValuePair<string, int>(function.Name, index));
                }
            }
        }

        private void LinkModule(int index)
        {
            ILModule module = _inputModules[index].Module;
            int baseIndex = _moduleOffsets[index];

            for (int i = 0, ilen = module.DefinedFunctions.Length; i < ilen; ++i) {
                ILFunction impl = _functionImplementations[baseIndex + i];
                LinkFunction(baseIndex, module, impl);
            }
        }

        private void LinkFunction(int baseIndex, ILModule module, ILFunction function)
        {
            int definedCount = module.DefinedFunctions.Length;

            foreach (int instructionIndex in function.InstructionsReferencingFunctionAddresses) {
                Instruction ins = function.Code[instructionIndex];

                if (ins.ImmediateValue < definedCount) {
                    // This is a call to a module local function that just needs to be offset
                    ins.ImmediateValue += baseIndex;
                } else {
                    // This is a function to an exported function defined outside of the module.
                    string functionName = module.UndefinedFunctionNames[ins.ImmediateValue - definedCount];

                    if (!_functionIndicesByName.TryGetValue(functionName, out ins.ImmediateValue)) {
                        // Function is currently undefined outside of the module.

                        int newIndex;
                        if (!_undefinedFunctionsIndicesByName.TryGetValue(functionName, out newIndex)) {
                            newIndex = _functionImplementations.Count + _undefinedFunctionsIndicesByName.Count;
                            _undefinedFunctionsIndicesByName[functionName] = newIndex;
                        }

                        ins.ImmediateValue = newIndex;
                    }
                }

                function.Code[instructionIndex] = ins;
            }

        }

    }
}
