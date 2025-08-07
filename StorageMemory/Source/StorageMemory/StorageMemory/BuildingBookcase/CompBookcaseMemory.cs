using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StorageMemory.BuildingBookcase
{
    public class CompBookcaseMemory : ThingComp
    {
        List<Thing> _cachedBooks = new List<Thing>();
        bool _useCachedBooks;
        
        public bool HasCachedBooks => _useCachedBooks && _cachedBooks?.Count > 0;

        public List<Thing> TakeCachedBooks()
        {
            var things = _cachedBooks.ToList();
            
            _cachedBooks.Clear();
            _useCachedBooks = false;
            
            return things;
        }
        
        public void PreCacheBooks(Building_Bookcase bookcase)
        {
#if DEBUG
            Log.Message("[StorageMemory] Pre-caching books from Bookcase");
#endif
            _useCachedBooks = true;
            _cachedBooks.Clear();
            var storedThings = new List<Thing> (bookcase.HeldBooks.Where(b => bookcase.Accepts(b)));

            foreach (var book in storedThings)
            {
                if (book == null || book.Destroyed) continue;
                
                _cachedBooks.Add(book.SplitOff(book.stackCount));
                
#if DEBUG
                Log.Message($"[StorageMemory] Stored book {book.LabelCap}.");
#endif
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
#if DEBUG
            Log.Message($"[StorageMemory] Spawning.");
#endif

            if (!_useCachedBooks || _cachedBooks == null || _cachedBooks.Count <= 0) return;

            var comp = parent as Building_Bookcase;
            if (comp == null) return;

            foreach (var gp in _cachedBooks.Where(gp => gp != null && !gp.Destroyed))
            {
                StorageMemoryManager.Instance.Enqueue(gp, comp.SearchableContents, parent.Position, parent.Map);
            }
            
            _cachedBooks.Clear();
            _useCachedBooks = false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref _useCachedBooks, "cachedBooks");
            Scribe_Collections.Look(ref _cachedBooks, "cachedBooks", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _cachedBooks == null)
            {
                _cachedBooks = new List<Thing>();
            }
        }
    }
}