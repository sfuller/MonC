using System.Collections.Generic;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.TypeSystem
{
    public class TypeManager
    {
        private readonly Dictionary<string, TypeGroup> _types = new Dictionary<string, TypeGroup>();

        public bool RegisterType(IValueType type)
        {
            if (_types.ContainsKey(type.Name)) {
                return false;
            }

            TypeGroup group = new TypeGroup {Value = type};
            _types[type.Name] = group;
            return true;
        }

        public IType? GetType(string name, PointerMode pointerMode = PointerMode.NotAPointer)
        {
            if (!_types.TryGetValue(name, out TypeGroup typeGroup)) {
                return null;
            }

            IType? type = typeGroup.GetTypeForPointerMode(pointerMode);
            if (type == null) {
                IPointerType pointerType = new PointerTypeImpl(typeGroup.Value, pointerMode);
                type = pointerType;
                typeGroup.SetTypeForPointerMode(pointerMode, pointerType);
                _types[name] = typeGroup;
            }

            return type;
        }

    }
}
