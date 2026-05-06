using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    /// <summary>Non-alloc geometry helpers for projectile swept tests.</summary>
    public static class ProjectileHitMath
    {
        public static float2 RotateDeg(float2 v, float degrees)
        {
            float rad = degrees * (math.PI / 180f);
            float c = math.cos(rad);
            float s = math.sin(rad);
            return new float2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>Alias for <see cref="RotateTowards2D"/> (plan naming).</summary>
        public static float2 SlerpDir2D(float2 from, float2 to, float maxAngleRad) =>
            RotateTowards2D(from, to, maxAngleRad);

        /// <summary>Rotate unit direction <paramref name="from"/> toward <paramref name="to"/> by at most <paramref name="maxAngleRad"/> radians.</summary>
        public static float2 RotateTowards2D(float2 from, float2 to, float maxAngleRad)
        {
            from = math.normalizesafe(from, new float2(0f, 1f));
            to = math.normalizesafe(to, new float2(0f, 1f));
            float signedAngle = math.atan2(to.y, to.x) - math.atan2(from.y, from.x);
            while (signedAngle > math.PI) signedAngle -= 2f * math.PI;
            while (signedAngle < -math.PI) signedAngle += 2f * math.PI;
            float clamped = math.clamp(signedAngle, -maxAngleRad, maxAngleRad);
            float cs = math.cos(clamped);
            float sn = math.sin(clamped);
            return new float2(from.x * cs - from.y * sn, from.x * sn + from.y * cs);
        }

        public static float2 Reflect(float2 dir, float2 normal)
        {
            float2 n = math.normalizesafe(normal, new float2(0f, 1f));
            float2 d = math.normalizesafe(dir, new float2(0f, 1f));
            return d - 2f * math.dot(d, n) * n;
        }

        /// <summary>First intersection of segment AB with circle (center, radius), t in [0,1] along AB. Uses inflated radius (projectile + target).</summary>
        public static bool SegmentVsCircle(float2 a, float2 b, float2 center, float radius, out float t, out float2 hitPoint)
        {
            t = 0f;
            hitPoint = a;
            float2 d = b - a;
            float lenSq = math.lengthsq(d);
            if (lenSq < 1e-10f)
                return math.distancesq(a, center) <= radius * radius;

            float2 oc = a - center;
            float aCoef = lenSq;
            float bCoef = 2f * math.dot(oc, d);
            float cCoef = math.dot(oc, oc) - radius * radius;

            if (cCoef <= 0f)
            {
                t = 0f;
                hitPoint = a;
                return true;
            }

            float disc = bCoef * bCoef - 4f * aCoef * cCoef;
            if (disc < 0f)
                return false;

            float sqrtDisc = math.sqrt(disc);
            float inv2a = 0.5f / aCoef;
            float t0 = (-bCoef - sqrtDisc) * inv2a;
            float t1 = (-bCoef + sqrtDisc) * inv2a;

            float tMin = float.MaxValue;
            if (t0 >= 0f && t0 <= 1f)
                tMin = math.min(tMin, t0);
            if (t1 >= 0f && t1 <= 1f)
                tMin = math.min(tMin, t1);

            if (tMin > 1f || tMin < 0f || tMin >= float.MaxValue * 0.5f)
                return false;

            t = tMin;
            hitPoint = a + d * t;
            return true;
        }

        public static float DistSqPointSegment(float2 p, float2 a, float2 b)
        {
            float2 ab = b - a;
            float den = math.dot(ab, ab);
            if (den < 1e-12f)
                return math.distancesq(p, a);
            float t = math.dot(p - a, ab) / den;
            t = math.clamp(t, 0f, 1f);
            float2 q = a + ab * t;
            return math.distancesq(p, q);
        }

        /// <summary>Inflated axis-aligned rectangle (center ± halfExtents + padding). Earliest hit t in [0,1].</summary>
        public static bool SegmentVsAABB(float2 a, float2 b, float2 center, float2 halfExtents, float padding,
            out float t, out float2 normal, out float2 hitPoint)
        {
            t = 0f;
            normal = new float2(0f, 1f);
            hitPoint = a;
            float2 half = halfExtents + new float2(padding, padding);
            float2 mn = center - half;
            float2 mx = center + half;
            float bestT = float.MaxValue;
            float2 bestN = new float2(0f, -1f);
            bool any = false;

            void TryVerticalFace(float xPlane, float nx)
            {
                float dx = b.x - a.x;
                if (math.abs(dx) < 1e-8f)
                    return;
                float u = (xPlane - a.x) / dx;
                if (u < 0f || u > 1f)
                    return;
                float y = a.y + u * (b.y - a.y);
                if (y < mn.y - 1e-4f || y > mx.y + 1e-4f)
                    return;
                if (u < bestT)
                {
                    bestT = u;
                    bestN = new float2(nx, 0f);
                    any = true;
                }
            }

            void TryHorizontalFace(float yPlane, float ny)
            {
                float dy = b.y - a.y;
                if (math.abs(dy) < 1e-8f)
                    return;
                float u = (yPlane - a.y) / dy;
                if (u < 0f || u > 1f)
                    return;
                float x = a.x + u * (b.x - a.x);
                if (x < mn.x - 1e-4f || x > mx.x + 1e-4f)
                    return;
                if (u < bestT)
                {
                    bestT = u;
                    bestN = new float2(0f, ny);
                    any = true;
                }
            }

            TryVerticalFace(mn.x, -1f);
            TryVerticalFace(mx.x, 1f);
            TryHorizontalFace(mn.y, -1f);
            TryHorizontalFace(mx.y, 1f);

            if (!any || bestT >= float.MaxValue * 0.5f)
                return false;

            t = bestT;
            float2 d = b - a;
            hitPoint = a + d * t;
            normal = bestN;
            return true;
        }

        /// <summary>Segment vs capsule: segment P1P2 expanded by radius <paramref name="r"/> (projectile radius).</summary>
        public static bool SegmentVsCapsule(float2 a, float2 b, float2 p1, float2 p2, float r, out float t,
            out float2 normal, out float2 hitPoint)
        {
            t = 0f;
            normal = new float2(0f, 1f);
            hitPoint = a;
            float r2 = r * r;
            float da = DistSqPointSegment(a, p1, p2);

            if (da <= r2)
            {
                t = 0f;
                hitPoint = a;
                float2 cw = ClosestPointOnSegment(a, p1, p2);
                normal = math.normalizesafe(a - cw, new float2(0f, 1f));
                return true;
            }

            bool crosses = false;
            for (int i = 0; i <= 32; i++)
            {
                float u = i / 32f;
                float2 p = math.lerp(a, b, u);
                if (DistSqPointSegment(p, p1, p2) <= r2)
                {
                    crosses = true;
                    break;
                }
            }

            if (!crosses)
                return false;

            float lo = 0f;
            float hi = 1f;
            for (int i = 0; i < 28; i++)
            {
                float mid = (lo + hi) * 0.5f;
                float2 pm = math.lerp(a, b, mid);
                if (DistSqPointSegment(pm, p1, p2) <= r2)
                    hi = mid;
                else
                    lo = mid;
            }

            t = hi;
            hitPoint = math.lerp(a, b, t);
            float2 cwall = ClosestPointOnSegment(hitPoint, p1, p2);
            normal = math.normalizesafe(hitPoint - cwall, new float2(0f, 1f));
            return true;
        }

        private static float2 ClosestPointOnSegment(float2 p, float2 a, float2 b)
        {
            float2 ab = b - a;
            float den = math.dot(ab, ab);
            if (den < 1e-12f)
                return a;
            float u = math.dot(p - a, ab) / den;
            u = math.clamp(u, 0f, 1f);
            return a + ab * u;
        }
    }
}
