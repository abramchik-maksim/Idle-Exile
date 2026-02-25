using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class CombatRenderer : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private Material _heroMaterial;
        [SerializeField] private Material _enemyMaterial;
        [SerializeField] private Material _projectileMaterial;

        [Header("Sizes")]
        [SerializeField] private float _heroScale = 0.8f;
        [SerializeField] private float _enemyScale = 0.6f;
        [SerializeField] private float _projectileScale = 0.15f;

        private Mesh _quadMesh;
        private Matrix4x4[] _matrices;
        private const int MaxInstances = 1023;

        private EntityQuery _heroQuery;
        private EntityQuery _enemyQuery;
        private EntityQuery _projectileQuery;
        private bool _initialized;

        private void Awake()
        {
            _quadMesh = CreateQuadMesh();
            _matrices = new Matrix4x4[MaxInstances];

            if (_heroMaterial == null)
                _heroMaterial = CreateDefaultMaterial(new Color(0.2f, 0.4f, 0.9f));
            if (_enemyMaterial == null)
                _enemyMaterial = CreateDefaultMaterial(new Color(0.9f, 0.2f, 0.2f));
            if (_projectileMaterial == null)
                _projectileMaterial = CreateDefaultMaterial(new Color(1f, 0.9f, 0.3f));
        }

        private void LateUpdate()
        {
            if (!TryInitialize()) return;

            DrawEntities(_heroQuery, _heroMaterial, _heroScale);
            DrawEntities(_enemyQuery, _enemyMaterial, _enemyScale);
            DrawEntities(_projectileQuery, _projectileMaterial, _projectileScale);
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

            _initialized = true;
            return true;
        }

        private void DrawEntities(EntityQuery query, Material material, float scale)
        {
            int count = query.CalculateEntityCount();
            if (count == 0) return;

            var positions = query.ToComponentDataArray<Position2D>(Unity.Collections.Allocator.Temp);

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

        private static Material CreateDefaultMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader);
            mat.color = color;
            mat.enableInstancing = true;
            return mat;
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                _heroQuery.Dispose();
                _enemyQuery.Dispose();
                _projectileQuery.Dispose();
            }
        }
    }
}
