using Project.Quadtree.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Project.Quadtree
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class QuadtreeGatewaySystem : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _endIniEcbs;
        private EntityCommandBuffer.ParallelWriter _endIniEcb;

        private QuadtreeCoreSystem _coreSystem;
        
        private int _sortKey = 0;
        private int NextSortKey => _sortKey++;
        
        private EntityQuery _removeQuery;
        private EntityQuery _addQuery;

        
        protected override void OnCreate()
        {
            _endIniEcbs = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            _coreSystem = World.GetOrCreateSystem<QuadtreeCoreSystem>();
            
            _removeQuery = GetEntityQuery(ComponentType.ReadOnly<RemoveTag>());
            _addQuery = GetEntityQuery(ComponentType.ReadOnly<AddTag>());
        }

        protected override void OnUpdate()
        {
            _endIniEcb = _endIniEcbs.CreateCommandBuffer().AsParallelWriter();
            RemoveUnits();
            AddUnits();
        }
        
        private void RemoveUnits()
        {
            Entities
                .WithAll<RemoveTag, NodeData>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity e) =>
                {
                    _coreSystem.NodesToCheckForMerge.Add(EntityManager.GetSharedComponentData<NodeData>(e));
                }).Run();

            var entities = _removeQuery.ToEntityArray(Allocator.TempJob);
            _endIniEcb.RemoveComponent<RemoveTag>(NextSortKey, entities);
            _endIniEcb.RemoveComponent<NodeData>(NextSortKey, entities);
            _endIniEcb.AddComponent<InPoolTag>(NextSortKey, entities);
            _endIniEcb.AddComponent<Disabled>(NextSortKey, entities);
            entities.Dispose(Dependency);
        }

        private void AddUnits()
        {
            Entities
                .WithAll<AddTag>()
                .WithoutBurst()
                .ForEach((Entity e, in Translation translation) =>
                {
                    var nodeData = _coreSystem.Tree.FindNodeData(translation.Value.xz);
                    _endIniEcb.AddSharedComponent(NextSortKey, e, nodeData);
                    _coreSystem.NodesToCheckForSplit.Add(nodeData);
                }).Run();
            
            var entities = _addQuery.ToEntityArray(Allocator.TempJob);
            _endIniEcb.RemoveComponent<AddTag>(NextSortKey, entities);
            entities.Dispose(Dependency);
        }
    }
}