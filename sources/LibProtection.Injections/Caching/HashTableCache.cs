﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace LibProtection.Injections.Caching
{
    internal sealed class HashTableCache<TKey, TValue>
    {
        private class CacheRecord
        {
            public TKey Key;
            public TValue Value;
        }

        private readonly CacheRecord[] _cache;
        private readonly IEqualityComparer<TKey> _comparer;

        public HashTableCache(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            this._cache = new CacheRecord[capacity];
            this._comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public TValue Get(TKey key, Func<TKey, TValue> factory)
        {
            var hashCode = unchecked((uint)_comparer.GetHashCode(key));
            var index = hashCode % _cache.Length;
            var record = Volatile.Read(ref _cache[index]);
            if (record == null || !_comparer.Equals(record.Key, key))
            {
                record = new CacheRecord
                {
                    Key = key,
                    Value = factory(key),
                };
                Volatile.Write(ref _cache[index], record);
            }
            return record.Value;
        }
    }

}
