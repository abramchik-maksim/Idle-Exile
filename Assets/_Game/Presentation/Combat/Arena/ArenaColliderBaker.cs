using System.Collections.Generic;
using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Presentation.Combat.Arena
{
    /// <summary>
    /// Runtime bake of Unity 2D colliders into ECS wall primitives.
    /// Assign <c>_colliderRoot</c> to the object that only has wall colliders (e.g. Grid/Tilemap Colliders), not unrelated arena children.
    /// </summary>
    public sealed class ArenaColliderBaker : MonoBehaviour
    {
        [SerializeField] private float _rdpEpsilon = ProjectileConstants.WallBakeRdpEpsilon;

        [Tooltip("If set, only colliders under this transform are baked (e.g. Grid/Tilemap Colliders). Avoids baking unrelated children of the arena root.")]
        [SerializeField] private Transform _colliderRoot;

        [Tooltip("CompositeCollider2D outlines often include interior horizontal chords (false 'floors'). Drop only nearly-horizontal edges whose midpoint is away from the world AABB faces (vertical walls are kept).")]
        [SerializeField] private bool _discardInteriorCompositeEdges = true;

        [SerializeField] private float _compositeEdgePerimeterMargin = 0.35f;

        [SerializeField]
        [Tooltip("When off, only a one-line bake summary is logged. Enable while tuning composite / tilemap colliders.")]
        private bool _verboseBakeLogs;

        private Transform ColliderSearchRoot => _colliderRoot != null ? _colliderRoot : transform;

        public void BakeIntoEntities(EntityManager entityManager, List<Entity> createdEntities)
        {
            createdEntities ??= new List<Entity>();
            createdEntities.Clear();

            var root = ColliderSearchRoot;
            bool verbose = _verboseBakeLogs;

            int beforeCount = createdEntities.Count;
            int compositeCount = 0;

            foreach (var composite in root.GetComponentsInChildren<CompositeCollider2D>(true))
            {
                compositeCount++;
                composite.GenerateGeometry();
                BakeCompositeCollider(composite, entityManager, createdEntities, verbose);
            }

            int afterComposite = createdEntities.Count;

            foreach (var box in root.GetComponentsInChildren<BoxCollider2D>(true))
            {
                if (box.usedByComposite)
                    continue;
                BakeBoxCollider(box, entityManager, createdEntities, verbose);
            }

            int afterBoxes = createdEntities.Count;

            foreach (var circle in root.GetComponentsInChildren<CircleCollider2D>(true))
            {
                if (circle.usedByComposite)
                    continue;
                BakeCircleCollider(circle, entityManager, createdEntities, verbose);
            }

            int afterCircles = createdEntities.Count;

            foreach (var poly in root.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                if (poly.usedByComposite)
                    continue;
                BakePolygonCollider(poly, entityManager, createdEntities, verbose);
            }

            int afterPolys = createdEntities.Count;
            int total = afterPolys - beforeCount;

            Debug.Log(
                $"[ArenaColliderBaker] Baked {total} wall entities under '{root.name}'. " +
                $"Composites={compositeCount} → edges={afterComposite - beforeCount}, " +
                $"BoxColliders → AABBs={afterBoxes - afterComposite}, " +
                $"CircleColliders → Circles={afterCircles - afterBoxes}, " +
                $"PolygonColliders → edges={afterPolys - afterCircles}.");
        }

        private void BakeCompositeCollider(CompositeCollider2D composite, EntityManager em, List<Entity> outs,
            bool verboseBakeLogs)
        {
            var tf = composite.transform;
            int pathCount = composite.pathCount;

            var simplifiedPaths = new List<List<Vector2>>(pathCount);
            var closedFlags = new List<bool>(pathCount);

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            for (int p = 0; p < pathCount; p++)
            {
                var path = new List<Vector2>();
                composite.GetPath(p, path);
                if (path.Count < 2)
                    continue;

                for (int i = 0; i < path.Count; i++)
                    path[i] = tf.TransformPoint(path[i]);

                bool closed = path.Count > 2 &&
                              Vector2.Distance(path[0], path[path.Count - 1]) < 0.001f;
                if (closed)
                    path.RemoveAt(path.Count - 1);

                List<Vector2> simplified = RamerDouglasPeucker(path, _rdpEpsilon);
                if (simplified.Count < 2)
                    continue;

                foreach (var pt in simplified)
                {
                    minX = Mathf.Min(minX, pt.x);
                    maxX = Mathf.Max(maxX, pt.x);
                    minY = Mathf.Min(minY, pt.y);
                    maxY = Mathf.Max(maxY, pt.y);
                }

                simplifiedPaths.Add(simplified);
                closedFlags.Add(closed);
            }

            bool bboxValid = minX <= maxX && minY <= maxY && float.IsFinite(minX);
            bool useInteriorFilter = _discardInteriorCompositeEdges && bboxValid;

            if (verboseBakeLogs)
                Debug.Log(
                    $"[ArenaColliderBaker] Composite '{composite.name}' pathCount={pathCount} " +
                    $"simplifiedPaths={simplifiedPaths.Count} aabb=[({minX:F2},{minY:F2}),({maxX:F2},{maxY:F2})] " +
                    $"interiorFilter={useInteriorFilter} margin={_compositeEdgePerimeterMargin:F2}.");

            for (int pi = 0; pi < simplifiedPaths.Count; pi++)
            {
                var simplified = simplifiedPaths[pi];
                bool closed = closedFlags[pi];
                int n = simplified.Count;
                int edgesCreated = 0;
                int edgesFiltered = 0;

                if (closed)
                {
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + 1) % n;
                        if (TryCreateWallEdge(
                                simplified[i], simplified[j], em, outs,
                                useInteriorFilter, minX, maxX, minY, maxY, _compositeEdgePerimeterMargin,
                                verboseBakeLogs))
                            edgesCreated++;
                        else
                            edgesFiltered++;
                    }
                }
                else
                {
                    for (int i = 0; i < n - 1; i++)
                    {
                        if (TryCreateWallEdge(
                                simplified[i], simplified[i + 1], em, outs,
                                useInteriorFilter, minX, maxX, minY, maxY, _compositeEdgePerimeterMargin,
                                verboseBakeLogs))
                            edgesCreated++;
                        else
                            edgesFiltered++;
                    }
                }

                if (verboseBakeLogs)
                    Debug.Log(
                        $"[ArenaColliderBaker]   Path #{pi} closed={closed} verts={n} created={edgesCreated} filtered={edgesFiltered}.");
            }
        }

        private static bool TryCreateWallEdge(
            Vector2 a,
            Vector2 b,
            EntityManager em,
            List<Entity> outs,
            bool useInteriorFilter,
            float minX,
            float maxX,
            float minY,
            float maxY,
            float margin,
            bool verboseBakeLogs)
        {
            if (useInteriorFilter && IsNearlyHorizontalEdge(a, b))
            {
                float mx = (a.x + b.x) * 0.5f;
                float my = (a.y + b.y) * 0.5f;
                if (!MidpointNearAabbBorder(mx, my, minX, maxX, minY, maxY, margin))
                {
                    if (verboseBakeLogs)
                        Debug.Log(
                            $"[ArenaColliderBaker]     FILTERED nearly-horizontal interior edge ({a.x:F2},{a.y:F2})→({b.x:F2},{b.y:F2}) midpoint=({mx:F2},{my:F2}).");
                    return false;
                }
            }

            CreateWallEdge(a, b, em, outs, verboseBakeLogs);
            return true;
        }

        private static bool IsNearlyHorizontalEdge(Vector2 a, Vector2 b)
        {
            float dy = Mathf.Abs(b.y - a.y);
            float dx = Mathf.Abs(b.x - a.x);
            return dy <= 0.02f && dx > 0.05f;
        }

        /// <summary>True if midpoint lies in a band along the outside of the composite axis-aligned bounds.</summary>
        private static bool MidpointNearAabbBorder(float mx, float my, float minX, float maxX, float minY, float maxY, float m)
        {
            return mx <= minX + m || mx >= maxX - m || my <= minY + m || my >= maxY - m;
        }

        private void BakePolygonCollider(PolygonCollider2D poly, EntityManager em, List<Entity> outs,
            bool verboseBakeLogs)
        {
            var tf = poly.transform;
            int pathCount = poly.pathCount;

            var simplifiedPaths = new List<List<Vector2>>(pathCount);
            var closedFlags = new List<bool>(pathCount);

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            for (int p = 0; p < pathCount; p++)
            {
                var path = new List<Vector2>();
                poly.GetPath(p, path);
                if (path.Count < 2)
                    continue;

                for (int i = 0; i < path.Count; i++)
                    path[i] = tf.TransformPoint(path[i]);

                bool closed = path.Count > 2 &&
                              Vector2.Distance(path[0], path[path.Count - 1]) < 0.001f;
                if (closed)
                    path.RemoveAt(path.Count - 1);

                List<Vector2> simplified = RamerDouglasPeucker(path, _rdpEpsilon);
                if (simplified.Count < 2)
                    continue;

                foreach (var pt in simplified)
                {
                    minX = Mathf.Min(minX, pt.x);
                    maxX = Mathf.Max(maxX, pt.x);
                    minY = Mathf.Min(minY, pt.y);
                    maxY = Mathf.Max(maxY, pt.y);
                }

                simplifiedPaths.Add(simplified);
                closedFlags.Add(closed);
            }

            bool bboxValid = minX <= maxX && minY <= maxY && float.IsFinite(minX);
            bool useInteriorFilter = _discardInteriorCompositeEdges && bboxValid;

            for (int pi = 0; pi < simplifiedPaths.Count; pi++)
            {
                var simplified = simplifiedPaths[pi];
                bool closed = closedFlags[pi];
                int n = simplified.Count;

                if (closed)
                {
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + 1) % n;
                        TryCreateWallEdge(
                            simplified[i], simplified[j], em, outs,
                            useInteriorFilter, minX, maxX, minY, maxY, _compositeEdgePerimeterMargin,
                            verboseBakeLogs);
                    }
                }
                else
                {
                    for (int i = 0; i < n - 1; i++)
                    {
                        TryCreateWallEdge(
                            simplified[i], simplified[i + 1], em, outs,
                            useInteriorFilter, minX, maxX, minY, maxY, _compositeEdgePerimeterMargin,
                            verboseBakeLogs);
                    }
                }
            }
        }

        private static void BakeBoxCollider(BoxCollider2D box, EntityManager em, List<Entity> outs, bool verboseBakeLogs)
        {
            var b = box.bounds;
            float2 c = new(b.center.x, b.center.y);
            float2 he = new(b.extents.x, b.extents.y);
            CreateWallAabb(c, he, em, outs, verboseBakeLogs);
        }

        private static void BakeCircleCollider(CircleCollider2D circle, EntityManager em, List<Entity> outs,
            bool verboseBakeLogs)
        {
            var b = circle.bounds;
            float2 c = new(b.center.x, b.center.y);
            float sx = Mathf.Abs(circle.transform.lossyScale.x);
            float sy = Mathf.Abs(circle.transform.lossyScale.y);
            float r = circle.radius * Mathf.Max(sx, sy);
            CreateWallCircle(c, r, em, outs, verboseBakeLogs);
        }

        private static void CreateWallEdge(Vector2 a, Vector2 b, EntityManager em, List<Entity> outs, bool verboseBakeLogs)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new WallTag());
            em.AddComponentData(e, new WallEdge { A = new float2(a.x, a.y), B = new float2(b.x, b.y) });
            float2 mid = (new float2(a.x, a.y) + new float2(b.x, b.y)) * 0.5f;
            em.AddComponentData(e, new Position2D { Value = mid });
            outs.Add(e);
            if (verboseBakeLogs)
                Debug.Log(
                    $"[ArenaColliderBaker]     EDGE entity={e} A=({a.x:F3},{a.y:F3}) B=({b.x:F3},{b.y:F3}) len={Vector2.Distance(a, b):F3}");
        }

        private static void CreateWallAabb(float2 center, float2 halfExtents, EntityManager em, List<Entity> outs,
            bool verboseBakeLogs)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new WallTag());
            em.AddComponentData(e, new WallAABB { HalfExtents = halfExtents });
            em.AddComponentData(e, new Position2D { Value = center });
            outs.Add(e);
            if (verboseBakeLogs)
                Debug.Log(
                    $"[ArenaColliderBaker]     AABB entity={e} center=({center.x:F3},{center.y:F3}) halfExtents=({halfExtents.x:F3},{halfExtents.y:F3})");
        }

        private static void CreateWallCircle(float2 center, float radius, EntityManager em, List<Entity> outs,
            bool verboseBakeLogs)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new WallTag());
            em.AddComponentData(e, new WallCircle { Radius = radius });
            em.AddComponentData(e, new Position2D { Value = center });
            outs.Add(e);
            if (verboseBakeLogs)
                Debug.Log(
                    $"[ArenaColliderBaker]     CIRCLE entity={e} center=({center.x:F3},{center.y:F3}) radius={radius:F3}");
        }

        private static List<Vector2> RamerDouglasPeucker(List<Vector2> points, float epsilon)
        {
            if (points.Count < 3)
                return new List<Vector2>(points);

            float sqEps = epsilon * epsilon;
            bool[] keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;
            SimplifyRange(points, 0, points.Count - 1, sqEps, keep);

            var result = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
            {
                if (keep[i])
                    result.Add(points[i]);
            }

            return result;
        }

        private static void SimplifyRange(List<Vector2> pts, int i0, int i1, float sqEps, bool[] keep)
        {
            if (i1 <= i0 + 1)
                return;

            Vector2 a = pts[i0];
            Vector2 b = pts[i1];
            float abx = b.x - a.x;
            float aby = b.y - a.y;
            float abLenSq = abx * abx + aby * aby;
            if (abLenSq < 1e-12f)
                return;

            int idx = -1;
            float maxSq = 0f;
            for (int i = i0 + 1; i < i1; i++)
            {
                float t = ((pts[i].x - a.x) * abx + (pts[i].y - a.y) * aby) / abLenSq;
                t = Mathf.Clamp01(t);
                float px = a.x + t * abx;
                float py = a.y + t * aby;
                float dx = pts[i].x - px;
                float dy = pts[i].y - py;
                float dSq = dx * dx + dy * dy;
                if (dSq > maxSq)
                {
                    maxSq = dSq;
                    idx = i;
                }
            }

            if (maxSq > sqEps && idx >= 0)
            {
                keep[idx] = true;
                SimplifyRange(pts, i0, idx, sqEps, keep);
                SimplifyRange(pts, idx, i1, sqEps, keep);
            }
        }
    }
}
