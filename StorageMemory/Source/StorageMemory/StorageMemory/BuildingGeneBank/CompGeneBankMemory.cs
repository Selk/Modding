using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StorageMemory.BuildingGeneBank
{
    public class CompGeneBankMemory : ThingComp
    {
        List<Thing> _cachedGenepacks = new List<Thing>();
        bool _useCachedGenepacks;
        
        public bool HasCachedGenepacks => _useCachedGenepacks && _cachedGenepacks?.Count > 0;

        public List<Thing> TakeCachedGenepacks()
        {
            var things = _cachedGenepacks.ToList();
            
            _cachedGenepacks.Clear();
            _useCachedGenepacks = false;
            
            return things;
        }
        
        public void PreCacheGenepacks(CompGenepackContainer compContainer)
        {
#if DEBUG
            Log.Message("[StorageMemory] Pre-caching Genepacks from Gene Bank");
#endif
            _useCachedGenepacks = true;
            _cachedGenepacks.Clear();
            
            var storedThings = new List<Genepack> (compContainer.ContainedGenepacks);
            
            foreach (var genepack in storedThings)
            {
                // Crée un GenePack factice avec un seul gène
                if (genepack == null || genepack.Destroyed) continue;
                var copy = genepack.SplitOff(genepack.stackCount) as Genepack;
                _cachedGenepacks.Add(copy);
#if DEBUG
                Log.Message($"[StorageMemory] Stored GenePack {genepack.GeneSet.Label}.");
#endif
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
#if DEBUG
            Log.Message($"[StorageMemory] Spawning.");
#endif

            if (!_useCachedGenepacks || _cachedGenepacks == null || _cachedGenepacks.Count <= 0) return;

            var comp = parent.TryGetComp<CompGenepackContainer>();
            if (comp == null) return;

            foreach (var gp in _cachedGenepacks)
            {
                if (gp == null || gp.Destroyed) continue;

                if (!comp.innerContainer.TryAdd(gp))
                {
#if DEBUG
                    Log.Warning($"[StorageMemory] Could not reinsert GenePack {gp.LabelCap}, placing on ground.");
#endif
                    GenPlace.TryPlaceThing(gp, parent.Position, parent.Map, ThingPlaceMode.Near);
                }
                else
                {
#if DEBUG
                    Log.Message($"[StorageMemory] Successfully reinserted GenePack: {gp.LabelCap}");
#endif
                }
            }
            
            _cachedGenepacks.Clear();
            _useCachedGenepacks = false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref _useCachedGenepacks, "useCachedGenepacks");
            Scribe_Collections.Look(ref _cachedGenepacks, "cachedGenepacks", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _cachedGenepacks == null)
            {
                _cachedGenepacks = new List<Thing>();
            }
        }
    }
}