using System.Collections.Generic;
using Verse;

namespace StorageMemory
{
    public class StorageMemoryManager : GameComponent
    {
        struct QueuedInsertion
        {
            public Thing thing;
            public ThingOwner container;
            public IntVec3 fallbackPos;
            public Map map;
        }

        readonly Queue<QueuedInsertion> _insertionQueue = new Queue<QueuedInsertion>();

        public StorageMemoryManager(Game game) { }

        public override void GameComponentTick()
        {
            if (_insertionQueue.Count > 0)
            {
                var q = _insertionQueue.Dequeue();

                if (q.container == null || !q.container.TryAdd(q.thing))
                {
                    GenPlace.TryPlaceThing(q.thing, q.fallbackPos, q.map, ThingPlaceMode.Near);
#if DEBUG
                Log.Warning($"[StorageMemory] Could not insert {q.thing.LabelCap}, dropped instead.");
#endif
                }
#if DEBUG
            else
            {
                Log.Message($"[StorageMemory] Deferred insert: {q.thing.LabelCap}");
            }
#endif
            }
        }

        public void Enqueue(Thing t, ThingOwner container, IntVec3 fallbackPos, Map map)
        {
            _insertionQueue.Enqueue(new QueuedInsertion
            {
                thing = t,
                container = container,
                fallbackPos = fallbackPos,
                map = map
            });
        }

        public static StorageMemoryManager Instance => Current.Game.GetComponent<StorageMemoryManager>();
    }
}