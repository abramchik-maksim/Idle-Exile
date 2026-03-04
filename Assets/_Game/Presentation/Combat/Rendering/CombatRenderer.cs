using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Application.Ports;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class CombatRenderer : MonoBehaviour, ICombatDisplaySettings
    {
        [Header("Materials")]
        [SerializeField] private Material _heroMaterial;
        [SerializeField] private Material _enemyMaterial;
        [SerializeField] private Material _projectileMaterial;
        [SerializeField] private Material _enemyProjectileMaterial;
        [SerializeField] private Material _meleeAoEMaterial;
        [SerializeField] private Material _spellAoEMaterial;
        [SerializeField] private Material _castBarBgMaterial;
        [SerializeField] private Material _castBarFillMaterial;
        [SerializeField] private Material _hpBarBgMaterial;
        [SerializeField] private Material _hpBarFillMaterial;
        [SerializeField] private Material _igniteMaterial;
        [SerializeField] private Material _chillMaterial;
        [SerializeField] private Material _shockMaterial;
        [SerializeField] private Material _bleedMaterial;
        [SerializeField] private Material _stunMaterial;
        [SerializeField] private Material _silenceMaterial;
        [SerializeField] private Material _cloneMaterial;
        [SerializeField] private Material _heroSlashMaterial;
        [SerializeField] private Material _enemySlashMaterial;

        [Header("Sizes")]
        [SerializeField] private float _heroScale = 0.8f;
        [SerializeField] private float _enemyScale = 0.6f;
        [SerializeField] private float _projectileScale = 0.15f;
        [SerializeField] private float _enemyProjectileScale = 0.2f;

        private Mesh _quadMesh;
        private Mesh _circleMesh;
        private Mesh _coneMesh;
        private Matrix4x4[] _matrices;
        private const int MaxInstances = 1023;
        private const int CircleSegments = 24;
        private const float ConeAngleDeg = 75f;
        private const int ConeSegments = 12;

        private EntityQuery _heroQuery;
        private EntityQuery _enemyQuery;
        private EntityQuery _projectileQuery;
        private EntityQuery _enemyProjectileQuery;
        private EntityQuery _meleeWindUpQuery;
        private EntityQuery _castingQuery;
        private EntityQuery _spellAoEQuery;
        private EntityQuery _enemyStatusQuery;
        private EntityQuery _cloneQuery;
        private EntityQuery _heroSlashQuery;
        private EntityQuery _enemySlashQuery;
        private Mesh _slashMesh;
        private bool _initialized;

        public bool ShowDamageNumbers { get; set; } = true;
        public bool ShowHpBars { get; set; } = true;
        public bool ShowEffectIndicators { get; set; } = true;

        private void Awake()
        {
            _quadMesh = CreateQuadMesh();
            _circleMesh = CreateCircleMesh(CircleSegments);
            _coneMesh = CreateConeMesh(ConeSegments, ConeAngleDeg);
            _matrices = new Matrix4x4[MaxInstances];

            if (_heroMaterial == null)
                _heroMaterial = CreateDefaultMaterial(new Color(0.2f, 0.4f, 0.9f));
            if (_enemyMaterial == null)
                _enemyMaterial = CreateDefaultMaterial(new Color(0.9f, 0.2f, 0.2f));
            if (_projectileMaterial == null)
                _projectileMaterial = CreateDefaultMaterial(new Color(1f, 0.9f, 0.3f));
            if (_enemyProjectileMaterial == null)
                _enemyProjectileMaterial = CreateDefaultMaterial(new Color(1f, 0.5f, 0.1f));
            if (_meleeAoEMaterial == null)
                _meleeAoEMaterial = CreateTransparentMaterial(new Color(0.9f, 0.15f, 0.1f, 0.25f));
            if (_spellAoEMaterial == null)
                _spellAoEMaterial = CreateTransparentMaterial(new Color(0.6f, 0.1f, 0.8f, 0.25f));
            if (_castBarBgMaterial == null)
                _castBarBgMaterial = CreateDefaultMaterial(new Color(0.15f, 0.15f, 0.15f));
            if (_castBarFillMaterial == null)
                _castBarFillMaterial = CreateDefaultMaterial(new Color(0.8f, 0.4f, 0.9f));
            if (_hpBarBgMaterial == null)
                _hpBarBgMaterial = CreateDefaultMaterial(new Color(0.2f, 0.05f, 0.05f));
            if (_hpBarFillMaterial == null)
                _hpBarFillMaterial = CreateDefaultMaterial(new Color(0.1f, 0.8f, 0.15f));
            if (_igniteMaterial == null)
                _igniteMaterial = CreateDefaultMaterial(new Color(1f, 0.5f, 0f));
            if (_chillMaterial == null)
                _chillMaterial = CreateDefaultMaterial(new Color(0.4f, 0.7f, 1f));
            if (_shockMaterial == null)
                _shockMaterial = CreateDefaultMaterial(new Color(1f, 1f, 0.3f));
            if (_bleedMaterial == null)
                _bleedMaterial = CreateDefaultMaterial(new Color(0.8f, 0.1f, 0.1f));
            if (_stunMaterial == null)
                _stunMaterial = CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
            if (_silenceMaterial == null)
                _silenceMaterial = CreateDefaultMaterial(new Color(0.6f, 0.2f, 0.8f));
            if (_cloneMaterial == null)
                _cloneMaterial = CreateDefaultMaterial(new Color(0.3f, 0.7f, 0.5f));
            if (_heroSlashMaterial == null)
                _heroSlashMaterial = CreateTransparentMaterial(new Color(1f, 1f, 1f, 0.85f));
            if (_enemySlashMaterial == null)
                _enemySlashMaterial = CreateTransparentMaterial(new Color(0.9f, 0.15f, 0.1f, 0.85f));

            _slashMesh = CreateSlashMesh();
        }

        private void LateUpdate()
        {
            if (!TryInitialize()) return;

            DrawEntities(_heroQuery, _heroMaterial, _heroScale);
            DrawEntities(_enemyQuery, _enemyMaterial, _enemyScale);
            DrawEntities(_cloneQuery, _cloneMaterial, _heroScale * 0.8f);
            DrawEntities(_projectileQuery, _projectileMaterial, _projectileScale);
            DrawEntities(_enemyProjectileQuery, _enemyProjectileMaterial, _enemyProjectileScale);
            DrawHeroSlashes();
            DrawEnemySlashes();
            DrawMeleeAoEZones();
            DrawSpellAoEZones();
            DrawCastBars();

            if (ShowHpBars)
                DrawEnemyHpBars();
            if (ShowEffectIndicators)
                DrawEffectIndicators();
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            var em = world.EntityManager;

            _heroQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<HeroTag>(),
                ComponentType.ReadOnly<Position2D>()
            );
            _enemyQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            _projectileQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<ProjectileTag>(),
                ComponentType.ReadOnly<Position2D>()
            );
            _enemyProjectileQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyProjectileTag>(),
                ComponentType.ReadOnly<Position2D>()
            );
            _meleeWindUpQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<MeleeWindUp>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            _castingQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<CastState>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            _spellAoEQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SpellAoE>()
            );
            _cloneQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<CloneTag>(),
                ComponentType.ReadOnly<Position2D>()
            );
            _enemyStatusQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.ReadOnly<CombatStats>(),
                ComponentType.ReadOnly<AilmentState>(),
                ComponentType.ReadOnly<StatusEffects>(),
                ComponentType.Exclude<DeadTag>()
            );
            _heroSlashQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<HeroSlashFX>()
            );
            _enemySlashQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemySlashFX>()
            );

            _initialized = true;
            return true;
        }

        private void DrawEntities(EntityQuery query, Material material, float scale)
        {
            int count = query.CalculateEntityCount();
            if (count == 0) return;

            var positions = query.ToComponentDataArray<Position2D>(Allocator.Temp);

            int batchStart = 0;
            while (batchStart < positions.Length)
            {
                int batchCount = math.min(positions.Length - batchStart, MaxInstances);
                for (int i = 0; i < batchCount; i++)
                {
                    var pos = positions[batchStart + i].Value;
                    _matrices[i] = Matrix4x4.TRS(
                        new Vector3(pos.x, pos.y, 0f),
                        Quaternion.identity,
                        new Vector3(scale, scale, 1f)
                    );
                }

                Graphics.DrawMeshInstanced(_quadMesh, 0, material, _matrices, batchCount);
                batchStart += batchCount;
            }

            positions.Dispose();
        }

        private void DrawMeleeAoEZones()
        {
            int count = _meleeWindUpQuery.CalculateEntityCount();
            if (count == 0) return;

            var positions = _meleeWindUpQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var windUps = _meleeWindUpQuery.ToComponentDataArray<MeleeWindUp>(Allocator.Temp);

            int batchStart = 0;
            while (batchStart < positions.Length)
            {
                int batchCount = math.min(positions.Length - batchStart, MaxInstances);
                for (int i = 0; i < batchCount; i++)
                {
                    var pos = positions[batchStart + i].Value;
                    var wu = windUps[batchStart + i];
                    float radius = wu.AoERadius;
                    float progress = math.clamp(wu.Timer / wu.Duration, 0f, 1f);
                    float pulse = 1f + progress * 0.15f;

                    float angle = math.atan2(wu.AoEDirection.y, wu.AoEDirection.x) * Mathf.Rad2Deg;
                    var rotation = Quaternion.Euler(0f, 0f, angle - 90f);

                    _matrices[i] = Matrix4x4.TRS(
                        new Vector3(pos.x, pos.y, -0.005f),
                        rotation,
                        new Vector3(radius * 2f * pulse, radius * 2f * pulse, 1f)
                    );
                }

                Graphics.DrawMeshInstanced(_coneMesh, 0, _meleeAoEMaterial, _matrices, batchCount);
                batchStart += batchCount;
            }

            positions.Dispose();
            windUps.Dispose();
        }

        private void DrawSpellAoEZones()
        {
            int count = _spellAoEQuery.CalculateEntityCount();
            if (count == 0) return;

            var spells = _spellAoEQuery.ToComponentDataArray<SpellAoE>(Allocator.Temp);

            int batchStart = 0;
            while (batchStart < spells.Length)
            {
                int batchCount = math.min(spells.Length - batchStart, MaxInstances);
                for (int i = 0; i < batchCount; i++)
                {
                    var s = spells[batchStart + i];
                    float radius = s.Radius;

                    _matrices[i] = Matrix4x4.TRS(
                        new Vector3(s.Center.x, s.Center.y, -0.005f),
                        Quaternion.identity,
                        new Vector3(radius * 2f, radius * 2f, 1f)
                    );
                }

                Graphics.DrawMeshInstanced(_circleMesh, 0, _spellAoEMaterial, _matrices, batchCount);
                batchStart += batchCount;
            }

            spells.Dispose();
        }

        private void DrawCastBars()
        {
            int count = _castingQuery.CalculateEntityCount();
            if (count == 0) return;

            var positions = _castingQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var casts = _castingQuery.ToComponentDataArray<CastState>(Allocator.Temp);

            const float barWidth = 0.6f;
            const float barHeight = 0.06f;
            const float barOffsetY = 0.55f;

            int bgIdx = 0;
            int fillIdx = 0;
            var bgMatrices = new Matrix4x4[math.min(count, MaxInstances)];
            var fillMatrices = new Matrix4x4[math.min(count, MaxInstances)];

            for (int i = 0; i < positions.Length && bgIdx < MaxInstances; i++)
            {
                if (casts[i].IsCasting == 0) continue;

                var pos = positions[i].Value;
                float progress = math.clamp(casts[i].CastTimer / casts[i].CastDuration, 0f, 1f);

                bgMatrices[bgIdx] = Matrix4x4.TRS(
                    new Vector3(pos.x, pos.y + barOffsetY, -0.01f),
                    Quaternion.identity,
                    new Vector3(barWidth, barHeight, 1f)
                );
                bgIdx++;

                float fillW = barWidth * progress;
                float fillX = pos.x - barWidth * 0.5f + fillW * 0.5f;

                fillMatrices[fillIdx] = Matrix4x4.TRS(
                    new Vector3(fillX, pos.y + barOffsetY, -0.02f),
                    Quaternion.identity,
                    new Vector3(fillW, barHeight, 1f)
                );
                fillIdx++;
            }

            if (bgIdx > 0)
            {
                Graphics.DrawMeshInstanced(_quadMesh, 0, _castBarBgMaterial, bgMatrices, bgIdx);
                Graphics.DrawMeshInstanced(_quadMesh, 0, _castBarFillMaterial, fillMatrices, fillIdx);
            }

            positions.Dispose();
            casts.Dispose();
        }

        private void DrawEnemyHpBars()
        {
            int count = _enemyStatusQuery.CalculateEntityCount();
            if (count == 0) return;

            var positions = _enemyStatusQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var statsArr = _enemyStatusQuery.ToComponentDataArray<CombatStats>(Allocator.Temp);

            const float barWidth = 0.5f;
            const float barHeight = 0.05f;
            const float barOffsetY = 0.45f;

            var bgMatrices = new Matrix4x4[math.min(count, MaxInstances)];
            var fillMatrices = new Matrix4x4[math.min(count, MaxInstances)];
            int idx = 0;

            for (int i = 0; i < positions.Length && idx < MaxInstances; i++)
            {
                float ratio = statsArr[i].MaxHealth > 0f
                    ? math.clamp(statsArr[i].CurrentHealth / statsArr[i].MaxHealth, 0f, 1f)
                    : 0f;

                if (ratio >= 1f) continue;

                var pos = positions[i].Value;

                bgMatrices[idx] = Matrix4x4.TRS(
                    new Vector3(pos.x, pos.y + barOffsetY, -0.01f),
                    Quaternion.identity,
                    new Vector3(barWidth, barHeight, 1f)
                );

                float fillW = barWidth * ratio;
                float fillX = pos.x - barWidth * 0.5f + fillW * 0.5f;

                fillMatrices[idx] = Matrix4x4.TRS(
                    new Vector3(fillX, pos.y + barOffsetY, -0.02f),
                    Quaternion.identity,
                    new Vector3(fillW, barHeight, 1f)
                );
                idx++;
            }

            if (idx > 0)
            {
                Graphics.DrawMeshInstanced(_quadMesh, 0, _hpBarBgMaterial, bgMatrices, idx);
                Graphics.DrawMeshInstanced(_quadMesh, 0, _hpBarFillMaterial, fillMatrices, idx);
            }

            positions.Dispose();
            statsArr.Dispose();
        }

        private void DrawEffectIndicators()
        {
            int count = _enemyStatusQuery.CalculateEntityCount();
            if (count == 0) return;

            var positions = _enemyStatusQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var ailments = _enemyStatusQuery.ToComponentDataArray<AilmentState>(Allocator.Temp);
            var statuses = _enemyStatusQuery.ToComponentDataArray<StatusEffects>(Allocator.Temp);

            const float iconSize = 0.1f;
            const float iconSpacing = 0.12f;
            const float iconOffsetY = 0.35f;

            using var igniteList = new NativeList<Matrix4x4>(Allocator.Temp);
            using var chillList = new NativeList<Matrix4x4>(Allocator.Temp);
            using var shockList = new NativeList<Matrix4x4>(Allocator.Temp);
            using var bleedList = new NativeList<Matrix4x4>(Allocator.Temp);
            using var stunList = new NativeList<Matrix4x4>(Allocator.Temp);
            using var silenceList = new NativeList<Matrix4x4>(Allocator.Temp);

            for (int i = 0; i < positions.Length; i++)
            {
                var pos = positions[i].Value;
                var ail = ailments[i];
                var st = statuses[i];

                int slot = 0;
                float baseX = pos.x;
                float y = pos.y + iconOffsetY;

                if (ail.IgniteTimer > 0f)
                    AddEffectIcon(igniteList, baseX, y, iconSize, iconSpacing, ref slot);
                if (ail.ChillStacks > 0)
                    AddEffectIcon(chillList, baseX, y, iconSize, iconSpacing, ref slot);
                if (ail.ShockStacks > 0)
                    AddEffectIcon(shockList, baseX, y, iconSize, iconSpacing, ref slot);
                if (ail.BleedTotalDps > 0f)
                    AddEffectIcon(bleedList, baseX, y, iconSize, iconSpacing, ref slot);
                if (st.Has(Game.Domain.Combat.StatusEffectType.Stun) || st.Has(Game.Domain.Combat.StatusEffectType.Frozen))
                    AddEffectIcon(stunList, baseX, y, iconSize, iconSpacing, ref slot);
                if (st.Has(Game.Domain.Combat.StatusEffectType.Silence))
                    AddEffectIcon(silenceList, baseX, y, iconSize, iconSpacing, ref slot);
            }

            DrawEffectBatch(igniteList, _igniteMaterial);
            DrawEffectBatch(chillList, _chillMaterial);
            DrawEffectBatch(shockList, _shockMaterial);
            DrawEffectBatch(bleedList, _bleedMaterial);
            DrawEffectBatch(stunList, _stunMaterial);
            DrawEffectBatch(silenceList, _silenceMaterial);

            positions.Dispose();
            ailments.Dispose();
            statuses.Dispose();
        }

        private static void AddEffectIcon(NativeList<Matrix4x4> list, float baseX, float y,
            float size, float spacing, ref int slot)
        {
            float x = baseX + (slot - 2.5f) * spacing;
            list.Add(Matrix4x4.TRS(
                new Vector3(x, y, -0.03f),
                Quaternion.identity,
                new Vector3(size, size, 1f)
            ));
            slot++;
        }

        private void DrawEffectBatch(NativeList<Matrix4x4> list, Material material)
        {
            if (list.Length == 0) return;
            int batchStart = 0;
            while (batchStart < list.Length)
            {
                int batchCount = math.min(list.Length - batchStart, MaxInstances);
                for (int i = 0; i < batchCount; i++)
                    _matrices[i] = list[batchStart + i];
                Graphics.DrawMeshInstanced(_quadMesh, 0, material, _matrices, batchCount);
                batchStart += batchCount;
            }
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "CombatQuad",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0, 0), new Vector2(1, 0),
                    new Vector2(1, 1), new Vector2(0, 1)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateConeMesh(int segments, float angleDeg)
        {
            float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;
            float startAngle = Mathf.PI * 0.5f - halfAngle;

            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            var uvs = new Vector2[segments + 2];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            float angleStep = (halfAngle * 2f) / segments;
            for (int i = 0; i <= segments; i++)
            {
                float a = startAngle + i * angleStep;
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f);
                uvs[i + 1] = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh
            {
                name = "CombatCone",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCircleMesh(int segments)
        {
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            var uvs = new Vector2[segments + 1];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            float angleStep = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                uvs[i + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh
            {
                name = "CombatCircle",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateDefaultMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;
            return mat;
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.enableInstancing = true;

            return mat;
        }

        private void DrawHeroSlashes()
        {
            int count = _heroSlashQuery.CalculateEntityCount();
            if (count == 0) return;

            var slashes = _heroSlashQuery.ToComponentDataArray<HeroSlashFX>(Allocator.Temp);
            int batchCount = math.min(count, MaxInstances);

            for (int i = 0; i < batchCount; i++)
            {
                var s = slashes[i];
                float t = s.Duration > 0f ? s.Timer / s.Duration : 1f;
                float alpha = 1f - t;

                float angle = math.atan2(s.Direction.y, s.Direction.x) * Mathf.Rad2Deg - 90f;

                float2 center = s.Origin + s.Direction * s.Length * 0.5f;

                _matrices[i] = Matrix4x4.TRS(
                    new Vector3(center.x, center.y, -0.01f),
                    Quaternion.Euler(0f, 0f, angle),
                    new Vector3(0.08f * alpha, s.Length, 1f)
                );
            }

            Graphics.DrawMeshInstanced(_slashMesh, 0, _heroSlashMaterial, _matrices, batchCount);
            slashes.Dispose();
        }

        private void DrawEnemySlashes()
        {
            int count = _enemySlashQuery.CalculateEntityCount();
            if (count == 0) return;

            var slashes = _enemySlashQuery.ToComponentDataArray<EnemySlashFX>(Allocator.Temp);
            int batchCount = math.min(count, MaxInstances);

            for (int i = 0; i < batchCount; i++)
            {
                var s = slashes[i];
                float t = s.Duration > 0f ? s.Timer / s.Duration : 1f;
                float alpha = 1f - t;

                float angle = math.atan2(s.Direction.y, s.Direction.x) * Mathf.Rad2Deg - 90f;
                float2 center = s.Origin + s.Direction * s.Length * 0.5f;

                _matrices[i] = Matrix4x4.TRS(
                    new Vector3(center.x, center.y, -0.01f),
                    Quaternion.Euler(0f, 0f, angle),
                    new Vector3(0.08f * alpha, s.Length, 1f)
                );
            }

            Graphics.DrawMeshInstanced(_slashMesh, 0, _enemySlashMaterial, _matrices, batchCount);
            slashes.Dispose();
        }

        private static Mesh CreateSlashMesh()
        {
            var mesh = new Mesh
            {
                name = "SlashLine",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3( 0.5f, -0.5f, 0f),
                    new Vector3( 0.5f,  0.5f, 0f),
                    new Vector3(-0.5f,  0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0, 0), new Vector2(1, 0),
                    new Vector2(1, 1), new Vector2(0, 1)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                _heroQuery.Dispose();
                _enemyQuery.Dispose();
                _projectileQuery.Dispose();
                _enemyProjectileQuery.Dispose();
                _meleeWindUpQuery.Dispose();
                _castingQuery.Dispose();
                _spellAoEQuery.Dispose();
                _enemyStatusQuery.Dispose();
                _cloneQuery.Dispose();
                _heroSlashQuery.Dispose();
                _enemySlashQuery.Dispose();
            }
        }
    }
}
