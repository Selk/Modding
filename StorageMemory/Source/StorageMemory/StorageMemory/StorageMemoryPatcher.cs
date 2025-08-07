using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using StorageMemory.BuildingBookcase;
using StorageMemory.BuildingGeneBank;
using StorageMemory.BuildingStorage;
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
                if (typeof(Building_Storage).IsAssignableFrom(def.thingClass))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();

                    if (!def.comps.Any(c => c is CompProperties_StorageMemory))
                    {
                        def.comps.Add(new CompProperties_StorageMemory());

#if DEBUG
                        Log.Message($"[StorageMemory] Add CompStorageMemory to {def.defName}");
#endif
                    }
                }
                else if (typeof(Building_Bookcase).IsAssignableFrom(def.thingClass))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    
                    if (!def.comps.Any(c => c is CompProperties_BookcaseMemory))
                    {
                        def.comps.Add(new CompProperties_BookcaseMemory());

#if DEBUG
                        Log.Message($"[BookcaseMemory] Add ComptBookcaseMemory to {def.defName}");
#endif
                    }
                }
                else if (def == ThingDefOf.GeneBank)
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    
                    if (!def.comps.Any(c => c is CompProperties_GeneBankMemory))
                    {
                        def.comps.Add(new CompProperties_GeneBankMemory());

#if DEBUG
                        Log.Message($"[StorageMemory] Add CompGeneBankMemory to {def.defName}");
#endif
                    }
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(Building_Bookcase), nameof(Building_Bookcase.DeSpawn))]
    public static class Patch_Bookcase_DeSpawn
    {
        public static void Prefix(Building_Bookcase __instance)
        {
            var comp = __instance.GetComp<CompBookcaseMemory>();
            if (comp != null)
            {
                comp.PreCacheBooks(__instance);
            }
        }
    }
    
    [HarmonyPatch(typeof(Building), nameof(Building.DeSpawn))]
    public static class Patch_GeneBank_DeSpawn
    {
        public static void Prefix(Building __instance)
        {
            var compContainer = __instance.TryGetComp<CompGenepackContainer>();

            if (compContainer == null) return;
            
            var comp = __instance.GetComp<CompGeneBankMemory>();
            comp?.PreCacheGenepacks(compContainer);
        }
    }
    
    [HarmonyPatch(typeof(MinifiedThing), nameof(MinifiedThing.Destroy))]
    public static class PatchMinifiedThingDestroy
    {
        static void DropCachedThings(ThingWithComps innerThing, List<Thing> things, string logMessage)
        {
            if (things == null) return;

            foreach (var thing in things)
            {
                GenPlace.TryPlaceThing(thing, innerThing.Position, innerThing.MapHeld, ThingPlaceMode.Near);
            }

#if DEBUG
            Log.Message($"[StorageMemory] {logMessage}");
#endif
        }
        
        public static void Prefix(MinifiedThing __instance)
        {
            if (__instance.InnerThing is Building_Storage building)
            {
                var comp = building.GetComp<CompStorageMemory>();
                
                if (comp == null || !comp.HasCachedThings) return;

                DropCachedThings(building, comp.TakeCachedThings(), "Dropped cached things on MinifiedThing destroy.");
            }
            else if (__instance.InnerThing is Building_Bookcase bookcase)
            {
                var comp = bookcase.GetComp<CompBookcaseMemory>();
                
                if (comp == null || !comp.HasCachedBooks) return;

                DropCachedThings(bookcase, comp.TakeCachedBooks(), "Dropped cached books on MinifiedThing destroy.");
            }
            else if (__instance.InnerThing is Building geneBank && geneBank.def == ThingDefOf.GeneBank)
            {
                var comp = geneBank.GetComp<CompGeneBankMemory>();
                
                if (comp == null || !comp.HasCachedGenepacks) return;

                DropCachedThings(geneBank, comp.TakeCachedGenepacks(), "Dropped cached genepacks on MinifiedThing destroy.");
            }
        }
    }
}