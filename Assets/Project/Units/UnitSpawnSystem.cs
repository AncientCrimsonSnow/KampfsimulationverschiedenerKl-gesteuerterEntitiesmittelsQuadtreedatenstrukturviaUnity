using Project.Quadtree;
using Project.Quadtree.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Console = Project.Development.Console;
using Random = UnityEngine.Random;

namespace Project.Units
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(QuadtreeGatewaySystem))]
    public partial class UnitSpawnSystem : SystemBase
    {
        private EntityQuery _poolQuery;
        private Console _console;

        protected override void OnCreate()
        {
            var poolQueryDesc = new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<InPoolTag>(), ComponentType.ReadOnly<Translation>() },
                None = new[] {ComponentType.ReadOnly<RemoveTag>()},
                Options = EntityQueryOptions.IncludeDisabled
            };
            _poolQuery = GetEntityQuery(poolQueryDesc);
        }

        protected override void OnStartRunning()
        {
            _console = Console.instance;
        }

        protected override void OnUpdate()
        {
            SpawnUnitsTeam1();
            SpawnUnitsTeam2();
        }

        private void SpawnUnitsTeam1()
        {
            if (_console.UnitsPerFrameTeam1 <= 0)
                return;

            var pos = new float2(1, Random.Range(0f, 100f));
            SpawnUnits(pos, _console.UnitsPerFrameTeam1, Teams.Team0);
        }

        private void SpawnUnitsTeam2()
        {
            if (_console.UnitsPerFrameTeam2 <= 0)
                return;
            
            var pos = new float2(99, Random.Range(0f, 100f));
            SpawnUnits(pos, _console.UnitsPerFrameTeam2, Teams.Team1);
        }

        private void SpawnUnits (float2 pos, int unitCount, Teams team)
        {
            var teamData = new TeamData
            {
                team = team
            };
            var unitPos = new Translation
            {
                Value = new float3(pos.x, 0, pos.y)
            };
            var entitiesFromPool = _poolQuery.ToEntityArray(Allocator.Temp);
            for (var i = 0; i != entitiesFromPool.Length; i++, unitCount--)
            {
                if (unitCount == 0)
                    return;
                
                EntityManager.SetComponentData(entitiesFromPool[i], unitPos);
                EntityManager.RemoveComponent<InPoolTag>(entitiesFromPool[i]);
                EntityManager.RemoveComponent<Disabled>(entitiesFromPool[i]);
                EntityManager.AddComponent<AddTag>(entitiesFromPool[i]);
                EntityManager.SetComponentData(entitiesFromPool[i], teamData);
            }
            
            var entitiesToSpawn = EntityManager.Instantiate(_console.unitToSpawnPrefab, unitCount, Allocator.Temp);
            foreach (var entity in entitiesToSpawn)
            {
                EntityManager.SetComponentData(entity, unitPos);
                EntityManager.AddComponent<AddTag>(entity);
                EntityManager.AddComponentData(entity, teamData);
            }
        }
    }
}