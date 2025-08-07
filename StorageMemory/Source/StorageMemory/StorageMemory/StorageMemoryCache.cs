using System.Collections.Generic;
using Verse;

namespace StorageMemory
{
    public static class StorageMemoryCache
    {
        public static readonly Dictionary<int, List<Thing>> CachedThings = new Dictionary<int, List<Thing>>();
    }
}