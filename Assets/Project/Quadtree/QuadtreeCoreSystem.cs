using System.Collections.Generic;
using System.Linq;
using Project.Quadtree.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Console = Project.Development.Console;

namespace Project.Quadtree
{
    /*
     *  EntityManager.CreateArchetype
     *
     *
     * 
     */
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class QuadtreeCoreSystem : SystemBase
    {
        //TODO change to not constant and to the real value of the chunk size
        private const uint NodeEntityCount = 47;
        public ECS_Quadtree Tree => _tree;

        public HashSet<NodeData> NodesToCheckForMerge => _nodesToCheckForMerge;

        public HashSet<NodeData> NodesToCheckForSplit => _nodesToCheckForSplit;

        private ECS_Quadtree _tree;
        
        private EntityQuery _nodeDataQuery;
        
        private readonly HashSet<NodeData> _nodesToCheckForMerge = new HashSet<NodeData>();

        private readonly HashSet<NodeData> _nodesToCheckForSplit = new HashSet<NodeData>();

        protected override void OnCreate()
        {
            var queryDesc = new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<NodeData>(), ComponentType.ReadOnly<Translation>()}
            };
            _nodeDataQuery = GetEntityQuery(queryDesc);
        }
        
        protected override void OnStartRunning()
        {
            _tree = new ECS_Quadtree(Console.instance.TotalBounds);
        }

        protected override void OnUpdate()
        {
            UpdateEntityNodes();
            CheckNodesForSplit();
            CheckNodesForMerge();
        }
        
        private void CheckNodesForSplit()
        {
            while (_nodesToCheckForSplit.Count != 0)
            {
                var nodeToCheck = _nodesToCheckForSplit.First();
                _nodeDataQuery.SetSharedComponentFilter(nodeToCheck);
                if (_nodeDataQuery.CalculateEntityCount() > NodeEntityCount)
                {
                    Split(_tree.GetNode(nodeToCheck));
                }
                _nodesToCheckForSplit.Remove(nodeToCheck);
            }
        }
        
        private void Split(ECS_Node node)
        {
            node.CreateChilds();
            var childs = node.Childs;
            var size = childs.Length;
            
            var childsData = new NativeArray<NodeData>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                childsData[i] = childs[i].data;
            }

            var center = node.data.center;
            
            Entities
                .WithStructuralChanges()
                .WithSharedComponentFilter(node.data)
                .WithoutBurst()
                .ForEach((Entity e, in Translation translation) =>
                {
                    var childIndex = ECS_Node.GetSplitBoundsIndex(translation.Value.xz, center);
                    EntityManager.SetSharedComponentData(e, childsData[childIndex]);
                }).Run();
            
            foreach (var child in node.Childs)
            {
                _nodesToCheckForSplit.Add(child.data);
            }
            
            childsData.Dispose(Dependency);
        }
        
        private void CheckNodesForMerge()
        {
            while (_nodesToCheckForMerge.Count!= 0)
            {
                var nodeToCheck = _nodesToCheckForMerge.First();
                if (nodeToCheck.index == 0)
                {
                    _nodesToCheckForMerge.Remove(nodeToCheck);
                }
                else
                {
                    _nodeDataQuery.SetSharedComponentFilter(nodeToCheck);
                    var nodesToRemove = CheckNodeForMerge(nodeToCheck);
                    _nodesToCheckForMerge.ExceptWith(nodesToRemove);
                }
            }
        }
        
        private HashSet<NodeData> CheckNodeForMerge(NodeData nodeData)
        {
            var (headNode, leafs) = GetHeadNodeAndLeafsToMerge(nodeData);
            if (nodeData != headNode)
            {
                Merge(headNode, leafs);
                ECS_Quadtree.ClearSubtree(_tree.GetNode(headNode));
            }
            leafs.Add(nodeData);
            return leafs;
        }
        
        private (NodeData, HashSet<NodeData>) GetHeadNodeAndLeafsToMerge(NodeData startNode)
        {
            var headNode = _tree.GetNode(startNode);
            var leafs = _tree.GetAllLeafsData(startNode);
            while (true)
            {
                if (headNode.data.index == 0)
                {
                    return (headNode.data, leafs);
                }

                var parent = headNode.parent;
                var parentLeafs = _tree.GetAllLeafsData(parent.data);
                
                var parentEntityCount = 0;
                foreach (var leaf in parentLeafs)
                {
                    _nodeDataQuery.SetSharedComponentFilter(leaf);
                    parentEntityCount += _nodeDataQuery.CalculateEntityCount();
                }

                if (parentEntityCount <= NodeEntityCount)
                {
                    headNode = parent;
                    leafs = parentLeafs;
                }
                else
                {
                    return (headNode.data, leafs);
                }
            }
        }

        private void Merge(NodeData head, HashSet<NodeData> leafs)
        {
            foreach (var leaf in leafs)
            {
                _nodeDataQuery.SetSharedComponentFilter(leaf);
                EntityManager.SetSharedComponentData(_nodeDataQuery, head);
            }
        }
        
        private void UpdateEntityNodes()
        {
            _nodeDataQuery.ResetFilter();
            var rootData = _tree.root.data;
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity e, in Translation translation) =>
                {
                    var nodeData = EntityManager.GetSharedComponentData<NodeData>(e);
                    var pos = translation.Value.xz;
                    if (!nodeData.IsInBounds(pos))
                    {
                        if (rootData.IsInBounds(pos))
                        {
                            _nodesToCheckForMerge.Add(nodeData);
                            var newNodeData = _tree.FindNodeData(pos);
                            _nodesToCheckForSplit.Add(newNodeData);
                            EntityManager.SetSharedComponentData(e, newNodeData);
                        }
                        else
                        {
                            EntityManager.AddComponent<RemoveTag>(e);
                        }
                    }
                }).Run();
        }
    }
}