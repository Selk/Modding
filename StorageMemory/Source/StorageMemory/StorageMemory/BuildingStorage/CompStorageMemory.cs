using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StorageMemory.BuildingStorage
{
    public class CompStorageMemory : ThingComp
    {
        List<Thing> _cachedThings = new List<Thing>();
        bool _useCachedThings;
        
        public bool HasCachedThings => _useCachedThings && _cachedThings?.Count > 0;

        public List<Thing> TakeCachedThings()
        {
            var things = _cachedThings.ToList();
            
            _cachedThings.Clear();
            _useCachedThings = false;
            
            return things;
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);

            if (mode == DestroyMode.KillFinalize || mode == DestroyMode.Vanish)
            {
                _useCachedThings = true;
                _cachedThings.Clear();
                var storedThings = new List<Thing>();

                if (!(parent is Building_Storage storage)) return;
                
#if DEBUG
                Log.Message($"[StorageMemory] parent is a Building_Storage");
#endif
                var containerComp = parent.TryGetComp<CompThingContainer>();
                if (containerComp != null)
                {
                    var heldThings = containerComp.GetDirectlyHeldThings();
                    storedThings.AddRange(heldThings);
                }
                else
                {
#if DEBUG
                    Log.Message($"[StorageMemory] No CompThingContainer found, checks directly on the ground.");
#endif
                    foreach (var cell in GenAdj.CellsOccupiedBy(parent))
                    {
                        var things = cell.GetThingList(map);
                        storedThings.AddRange(things.Where(thing => storage.Accepts(thing)));
                    }
                }
                
                CacheThings(storedThings);
            }
            else
            {
                _useCachedThings = false;
            }
        }

        void CacheThings(List<Thing> things)
        {
            foreach (var thing in things)
            {
                if (thing == null || thing.Destroyed) continue;
#if DEBUG
                Log.Message($"[StorageMemory] Stored {thing.LabelCap} item.");
#endif
                var copy = thing.SplitOff(thing.stackCount);
                _cachedThings.Add(copy);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
#if DEBUG
            Log.Message($"[StorageMemory] Spawning.");
#endif

            if (!_useCachedThings || _cachedThings == null || _cachedThings.Count <= 0) return;
            
            var containerComp = parent.TryGetComp<CompThingContainer>();
            if (containerComp != null)
            {
                var helder = containerComp.GetDirectlyHeldThings();

                foreach (var thing in _cachedThings.Where(thing => thing != null && !thing.Destroyed))
                {
                    StorageMemoryManager.Instance.Enqueue(thing, helder, parent.Position, parent.Map);
                }
            }
            else
            {
#if DEBUG
                Log.Message($"[StorageMemory] No containerComp found, add directly on the ground.");
#endif
                foreach (var thing in _cachedThings.Where(t => t != null && !t.Destroyed))
                {
                    StorageMemoryManager.Instance.Enqueue(thing, null, parent.Position, parent.Map);
                }
            }

            _cachedThings?.Clear();
            _useCachedThings = false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref _useCachedThings, "useCachedThings");
            Scribe_Collections.Look(ref _cachedThings, "cachedThings", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _cachedThings == null)
            {
                _cachedThings = new List<Thing>();
            }
        }
    }
}