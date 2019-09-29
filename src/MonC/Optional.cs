using System;
using MonC.SyntaxTree;

namespace MonC
{
    public struct Optional<T> where T : class
    {
        private readonly T _item;
        
        public Optional(T item)
        {
#if DEBUG
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }            
#endif
            
            _item = item;
        }

        public bool Get(out T item)
        {
            item = _item;
            return item != null;
        }

        public Optional<BT> Abstract<BT>() where BT : class
        {
            if (_item == null) {
                return new Optional<BT>();
            }
            
            // NOTE: Thanks to the null check, the JIT/AOT compiler may be able to optimize out the type check invoked
            // by `item as BT`
            return new Optional<BT>(_item as BT);
        }

    }
}