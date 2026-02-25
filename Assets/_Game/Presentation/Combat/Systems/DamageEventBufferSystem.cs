using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class DamageEventBufferSystem : SystemBase
    {
        public NativeQueue<DamageEvent> EventQueue;
        public readonly List<DamageEvent> FrameEvents = new();

        protected override void OnCreate()
        {
            EventQueue = new NativeQueue<DamageEvent>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (EventQueue.IsCreated)
                EventQueue.Dispose();
        }

        protected override void OnUpdate()
        {
            FrameEvents.Clear();
            while (EventQueue.TryDequeue(out var evt))
                FrameEvents.Add(evt);
        }
    }
}
