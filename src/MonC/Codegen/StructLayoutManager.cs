using System;
using System.Collections.Generic;
using MonC.TypeSystem.Types.Impl;

namespace MonC.Codegen
{
    public class StructLayoutManager
    {
        private StructLayoutGenerator? _generator;
        private readonly Dictionary<StructType, StructLayout> _layouts = new Dictionary<StructType, StructLayout>();

        private readonly HashSet<StructType> _structsBeingGenerated = new HashSet<StructType>();

        public void Setup(StructLayoutGenerator generator)
        {
            _generator = generator;
        }

        public StructLayout GetLayout(StructType structType)
        {
            if (_generator == null) {
                throw new InvalidOperationException("Must call Setup() first.");
            }

            if (_layouts.TryGetValue(structType, out StructLayout layout)) {
                return layout;
            }

            if (_structsBeingGenerated.Contains(structType)) {
                throw new InvalidOperationException("Cycle detected");
            }
            _structsBeingGenerated.Add(structType);

            layout = _generator.Generate(structType, this);
            _layouts.Add(structType, layout);

            _structsBeingGenerated.Remove(structType);

            return layout;
        }


    }
}
