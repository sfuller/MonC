using System;
using System.Collections.Generic;
using MonC.TypeSystem.Types;
using MonC.TypeSystem.Types.Impl;

namespace MonC.TypeSystem
{
    public class TypeManager
    {
        private readonly Dictionary<IValueType, TypeGroup> _valueTypes = new Dictionary<IValueType, TypeGroup>();
        private readonly Dictionary<string, IValueType> _nameToValueType = new Dictionary<string, IValueType>();

        public bool RegisterType(IValueType type)
        {
            if (_nameToValueType.ContainsKey(type.Name)) {
                return false;
            }

            TypeGroup group = new TypeGroup {Value = type};
            _valueTypes[type] = group;
            _nameToValueType[type.Name] = type;
            return true;
        }

        public IType? GetType(string name, PointerMode pointerMode = PointerMode.NotAPointer)
        {
            if (!_nameToValueType.TryGetValue(name, out IValueType valueType)) {
                return null;
            }

            return GetType(valueType, pointerMode);
        }

        public IType GetType(IValueType valueType, PointerMode pointerMode)
        {
            if (!_valueTypes.TryGetValue(valueType, out TypeGroup typeGroup)) {
                throw new ArgumentException("Value type is not registered", nameof(valueType));
            }

            IType? type = typeGroup.GetTypeForPointerMode(pointerMode);
            if (type == null) {
                IPointerType pointerType = new PointerTypeImpl(typeGroup.Value, pointerMode);
                type = pointerType;
                typeGroup.SetTypeForPointerMode(pointerMode, pointerType);
                _valueTypes[valueType] = typeGroup;
            }

            return type;
        }

    }
}
