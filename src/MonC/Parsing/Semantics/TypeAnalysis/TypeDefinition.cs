using System;
using MonC.SyntaxTree;

namespace MonC.Parsing.Semantics.TypeAnalysis
{
    public readonly struct TypeDefinition : IEquatable<TypeDefinition>
    {
        public readonly string? Name;
        public readonly PointerType PointerType;
        
        public TypeDefinition(string name, PointerType pointerType)
        {
            Name = name;
            PointerType = pointerType;
        }
        
        public TypeDefinition(TypeSpecifier leaf)
        {
            Name = leaf.Name;
            PointerType = leaf.PointerType;
        }
        
        public bool Equals(TypeDefinition other)
        {
            return Name == other.Name && PointerType == other.PointerType;
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((Name ?? "").GetHashCode() * 397) ^ (int)PointerType;
            }
        }

        public static bool operator ==(TypeDefinition a, TypeDefinition b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(TypeDefinition a, TypeDefinition b)
        {
            return !a.Equals(b);
        }
    }
}