using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StorageMemory
{
    [StaticConstructorOnStartup]
    public static class StorageMemoryPatcher
    {
        static StorageMemoryPatcher()
        {
            var harmony = new Harmony("Karshou.StorageMemory");
            harmony.PatchAll();

            AddMemoryCompToStorageBuildings();
        }

        static void AddMemoryCompToStorageBuildings()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!typeof(Building_Storage).IsAssignableFrom(def.thingClass)) continue;
                
                if (def.comps == null) def.comps = new List<CompProperties>();

                if (!def.comps.Any(c => c is CompProperties_StorageMemory))
                {
                    def.comps.Add(new CompProperties_StorageMemory());

#if DEBUG
                    Log.Message($"[StorageMemory] Add to {def.defName}");
#endif
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(MinifiedThing), nameof(MinifiedThing.Destroy))]
    public static class PatchMinifiedThingDestroy
    {
        public static void Prefix(MinifiedThing __instance)
        {
            if (!(__instance.InnerThing is Building_Storage building)) return;

            var comp = building.GetComp<CompStorageMemory>();
            
            if (comp == null || !comp.HasCachedThings) return;

            foreach (var thing in comp.TakeCachedThings())
            {
                GenPlace.TryPlaceThing(thing, __instance.Position, __instance.MapHeld, ThingPlaceMode.Near);
            }

#if DEBUG
            Log.Message($"[StorageMemory] Dropped cached things on MinifiedThing destroy.");
#endif
        }
    }
}