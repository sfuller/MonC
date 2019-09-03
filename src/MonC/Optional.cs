using System;

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

    }
}