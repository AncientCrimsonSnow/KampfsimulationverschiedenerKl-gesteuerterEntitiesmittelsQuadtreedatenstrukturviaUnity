using System;
using System.Collections.Generic;
using Project.Quadtree;
using Unity.Collections;
using Unity.Mathematics;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.DrawPerspectiveStates
{
    public abstract class DrawPerspectiveBaseState : IDisposable
    {
        public NativeArray<PathNode> Leafs
        {
            get => pathState.leafs;
            set => pathState.leafs = value;
        }

        public Dictionary<ECS_Node, int> IndexMap
        {
            set => pathState.indexMap = value;
        }
        
        public NativeArray<byte> FlowField
        {
            get => pathState.flowFieldState.flowField;
            set => pathState.flowFieldState.flowField = value;
        }
        
        public float2 target;
        public ECS_Node endNode;
        public readonly DebugDrawer drawer;
        public ECS_Quadtree Tree => drawer.Tree;
        
        public NativeMultiHashMap<int, int> pathGraph;
        public NativeList<(float2, float2, float2)>  bezierCurves;
        private Dictionary<float2, List<float2>> _pathTree;
        private int _pathTreeHash = 0;
        
        protected readonly DrawPathState pathState;
        
        protected DrawPerspectiveBaseState(
            DebugDrawer drawer,
            DrawPathState pathState)
        {
            this.drawer = drawer;
            this.pathState = pathState;
        }

        public HashSet<float4> EndNodeEdges { get; } = new HashSet<float4>();

        public abstract void Enter();
        public abstract void Update();
        
        public void SetEndNode(ECS_Node endMode)
        {
            endNode = endMode;
            EndNodeEdges.Clear();
            foreach (var edge in endNode.data.GetEdges())
            {
                EndNodeEdges.Add(edge);
            }
        }
        
        public Dictionary<float2, List<float2>> GetPathTree(NativeArray<PathNode> leafs)
        {
            var hash = pathGraph.GetHashCode();
            if (hash == _pathTreeHash)
                return _pathTree;

            _pathTreeHash = hash;
            var result = new Dictionary<float2, List<float2>>();

            using (var enumerator = pathGraph.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Current;
                    if(kvp.Value == -1)
                        continue;
                    
                    var node1Center = Tree.AllNodes[leafs[kvp.Key].nodeIndexInTree].data.center;
                    float2 node2Center;
                    if (leafs[kvp.Value].nodeIndexInTree == endNode.data.index)
                    {
                        node2Center = target;
                    }
                    else
                    {
                        node2Center = Tree.AllNodes[leafs[kvp.Value].nodeIndexInTree].data.center;
                    }
                    
                    if(result.ContainsKey(node1Center))
                        result[node1Center].Add(node2Center);
                    else
                    {
                        result.Add(node1Center, new List<float2>
                        {
                            node2Center
                        });
                    }
                }
            }

            _pathTree = result;
            return result;
        }

        public void Dispose()
        {
            if(pathGraph.IsCreated)
                pathGraph.Dispose();
            if(bezierCurves.IsCreated)
                bezierCurves.Dispose();
        }
    }
}