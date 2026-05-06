using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Presentation.Combat.Systems
{
    /// <summary>
    /// Diagnostic: draws every baked <see cref="WallTag"/> entity each frame using <see cref="Debug.DrawLine"/>
    /// so phantom walls are visible in Scene view (and Game view when Gizmos toggle is on).
    /// Edges = magenta, AABB rectangles = yellow, circles approximated with 24 segments = cyan.
    /// Toggle via <see cref="ProjectileLifetimeDebug.HeroProjectileLogsEnabled"/>.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class WallDebugRenderSystem : SystemBase
    {
        private static readonly Color EdgeColor = Color.magenta;
        private static readonly Color AabbColor = Color.yellow;
        private static readonly Color CircleColor = Color.cyan;
        private static readonly Color StuckProjectileColor = new Color(1f, 0.4f, 1f, 1f);

        private const int CircleSegments = 24;

        protected override void OnCreate()
        {
            RequireForUpdate<WallTag>();
        }

        protected override void OnUpdate()
        {
            if (!ProjectileLifetimeDebug.HeroProjectileLogsEnabled)
                return;

            foreach (var edge in SystemAPI.Query<RefRO<WallEdge>>().WithAll<WallTag>())
            {
                float3 a3 = new(edge.ValueRO.A.x, edge.ValueRO.A.y, 0f);
                float3 b3 = new(edge.ValueRO.B.x, edge.ValueRO.B.y, 0f);
                Debug.DrawLine(a3, b3, EdgeColor);
            }

            foreach (var (pos, box) in SystemAPI.Query<RefRO<Position2D>, RefRO<WallAABB>>().WithAll<WallTag>())
            {
                float2 c = pos.ValueRO.Value;
                float2 he = box.ValueRO.HalfExtents;
                float3 bl = new(c.x - he.x, c.y - he.y, 0f);
                float3 br = new(c.x + he.x, c.y - he.y, 0f);
                float3 tr = new(c.x + he.x, c.y + he.y, 0f);
                float3 tl = new(c.x - he.x, c.y + he.y, 0f);
                Debug.DrawLine(bl, br, AabbColor);
                Debug.DrawLine(br, tr, AabbColor);
                Debug.DrawLine(tr, tl, AabbColor);
                Debug.DrawLine(tl, bl, AabbColor);
            }

            foreach (var (pos, circ) in SystemAPI.Query<RefRO<Position2D>, RefRO<WallCircle>>().WithAll<WallTag>())
            {
                DrawCircle(pos.ValueRO.Value, circ.ValueRO.Radius, CircleColor);
            }

            foreach (var (pos, proj) in SystemAPI.Query<RefRO<Position2D>, RefRO<ProjectileData>>().WithAll<ProjectileTag>())
            {
                if (proj.ValueRO.MotionMode != ProjectileMotionMode.StuckInWall)
                    continue;
                DrawCircle(pos.ValueRO.Value, 0.15f, StuckProjectileColor);
                float3 c = new(pos.ValueRO.Value.x, pos.ValueRO.Value.y, 0f);
                Debug.DrawLine(c + new float3(-0.1f, 0f, 0f), c + new float3(0.1f, 0f, 0f), StuckProjectileColor);
                Debug.DrawLine(c + new float3(0f, -0.1f, 0f), c + new float3(0f, 0.1f, 0f), StuckProjectileColor);
            }
        }

        private static void DrawCircle(float2 center, float radius, Color color)
        {
            float twoPi = 2f * math.PI;
            float prevX = center.x + radius;
            float prevY = center.y;
            for (int i = 1; i <= CircleSegments; i++)
            {
                float t = (i / (float)CircleSegments) * twoPi;
                float x = center.x + math.cos(t) * radius;
                float y = center.y + math.sin(t) * radius;
                Debug.DrawLine(new Vector3(prevX, prevY, 0f), new Vector3(x, y, 0f), color);
                prevX = x;
                prevY = y;
            }
        }
    }
}
