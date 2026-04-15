using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Application.Ports;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class CombatOverlayRenderer : MonoBehaviour, ICombatDisplaySettings
    {
        [Header("Overlay Materials")]
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
        [SerializeField] private Material _heroSlashMaterial;
        [SerializeField] private Material _enemySlashMaterial;

        private Mesh _quadMesh;
        private Mesh _circleMesh;
        private Mesh _coneMesh;
        private Mesh _slashMesh;
        private Matrix4x4[] _matrices;
        private const int MaxInstances = 1023;
        private const int CircleSegments = 24;
        private const float ConeAngleDeg = 75f;
        private const int ConeSegments = 12;

        private EntityQuery _meleeWindUpQuery;
        private EntityQuery _castingQuery;
        private EntityQuery _spellAoEQuery;
        private EntityQuery _enemyStatusQuery;
        private EntityQuery _heroSlashQuery;
        private EntityQuery _enemySlashQuery;
        private bool _initialized;

        public bool ShowDamageNumbers { get; set; } = true;
        public bool ShowHpBars { get; set; } = true;
        public bool ShowEffectIndicators { get; set; } = true;

        private void Awake()
        {
            _quadMesh = CombatMeshFactory.CreateQuad();
            _circleMesh = CombatMeshFactory.CreateCircle(CircleSegments);
            _coneMesh = CombatMeshFactory.CreateCone(ConeSegments, ConeAngleDeg);
            _slashMesh = CombatMeshFactory.CreateSlash();
            _matrices = new Matrix4x4[MaxInstances];

            _meleeAoEMaterial = CombatMaterialFactory.EnsureOrCreate(_meleeAoEMaterial, new Color(0.9f, 0.15f, 0.1f, 0.25f), true);
            _spellAoEMaterial = CombatMaterialFactory.EnsureOrCreate(_spellAoEMaterial, new Color(0.6f, 0.1f, 0.8f, 0.25f), true);
            _castBarBgMaterial = CombatMaterialFactory.EnsureOrCreate(_castBarBgMaterial, new Color(0.15f, 0.15f, 0.15f));
            _castBarFillMaterial = CombatMaterialFactory.EnsureOrCreate(_castBarFillMaterial, new Color(0.8f, 0.4f, 0.9f));
            _hpBarBgMaterial = CombatMaterialFactory.EnsureOrCreate(_hpBarBgMaterial, new Color(0.2f, 0.05f, 0.05f));
            _hpBarFillMaterial = CombatMaterialFactory.EnsureOrCreate(_hpBarFillMaterial, new Color(0.1f, 0.8f, 0.15f));
            _igniteMaterial = CombatMaterialFactory.EnsureOrCreate(_igniteMaterial, new Color(1f, 0.5f, 0f));
            _chillMaterial = CombatMaterialFactory.EnsureOrCreate(_chillMaterial, new Color(0.4f, 0.7f, 1f));
            _shockMaterial = CombatMaterialFactory.EnsureOrCreate(_shockMaterial, new Color(1f, 1f, 0.3f));
            _bleedMaterial = CombatMaterialFactory.EnsureOrCreate(_bleedMaterial, new Color(0.8f, 0.1f, 0.1f));
            _stunMaterial = CombatMaterialFactory.EnsureOrCreate(_stunMaterial, new Color(0.5f, 0.5f, 0.5f));
            _silenceMaterial = CombatMaterialFactory.EnsureOrCreate(_silenceMaterial, new Color(0.6f, 0.2f, 0.8f));
            _heroSlashMaterial = CombatMaterialFactory.EnsureOrCreate(_heroSlashMaterial, new Color(1f, 1f, 1f, 0.85f), true);
            _enemySlashMaterial = CombatMaterialFactory.EnsureOrCreate(_enemySlashMaterial, new Color(0.9f, 0.15f, 0.1f, 0.85f), true);
        }

        private void LateUpdate()
        {
            if (!TryInitialize()) return;

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

        private void OnDestroy()
        {
            if (_initialized)
            {
                _meleeWindUpQuery.Dispose();
                _castingQuery.Dispose();
                _spellAoEQuery.Dispose();
                _enemyStatusQuery.Dispose();
                _heroSlashQuery.Dispose();
                _enemySlashQuery.Dispose();
            }
        }
    }
}
