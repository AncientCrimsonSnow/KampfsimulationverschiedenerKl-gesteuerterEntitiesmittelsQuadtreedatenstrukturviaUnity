using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Quadtree.AStart
{
    public static class FlowFieldUtils
    {
        private const float TwoPi = Mathf.PI * 2;
        public static (NativeArray<PathNode>, NativeMultiHashMap<int, int> , Dictionary<ECS_Node, int>, NativeHashMap<int, float2>, NativeMultiHashMap<int, float4>) GetCreateFlowFieldJobData(HashSet<ECS_Node> leafs)
        {
            var allNodesCount = leafs.Count;
            
            var indexMap = new Dictionary<ECS_Node, int>();
            var neighbourIndices =  new NativeMultiHashMap<int, int>(allNodesCount, Allocator.Persistent);
            
            var allNodes = new NativeArray<PathNode>(allNodesCount, Allocator.Persistent);
            var indexEdgePointer = new NativeMultiHashMap<int, float4>(allNodesCount, Allocator.Persistent);
            var index = 0;
            foreach (var leaf in leafs)
            {
                foreach (var edge in leaf.data.GetEdges())
                    indexEdgePointer.Add(index, edge);
                
                indexMap.Add(leaf, index);
                index++;
            }

            foreach (var leaf in indexMap)
            {
                allNodes[leaf.Value] = new PathNode
                {
                    nodeIndexInTree = leaf.Key.data.index,
                    nodeIndexInLeafs = leaf.Value,
                };
                var indicesArray = NeighboursNodesToIndicesArray(leaf.Key.neighbourNodes, indexMap);
                foreach (var nodeIndex in indicesArray)
                    neighbourIndices.Add(leaf.Value, nodeIndex);
            }
            
            var indexCenterPointer = new NativeHashMap<int, float2>(indexMap.Count, Allocator.Persistent);
            foreach (var indexEntry in indexMap)
            {
                indexCenterPointer.Add(indexEntry.Value, indexEntry.Key.data.center);
            }
            
            
            return (allNodes, neighbourIndices, indexMap, indexCenterPointer, indexEdgePointer);
        }

        private static IEnumerable<int> NeighboursNodesToIndicesArray(NodeNeighbours neighboursData, IReadOnlyDictionary<ECS_Node, int> indexMap)
        {
            var result = new List<int>();
            if (!(neighboursData.botSide is null)) 
                result.AddRange(neighboursData.botSide.Select(node => indexMap[node]));
            if (!(neighboursData.leftSide is null)) 
                result.AddRange(neighboursData.leftSide.Select(node => indexMap[node]));
            if (!(neighboursData.topSide is null)) 
                result.AddRange(neighboursData.topSide.Select(node => indexMap[node]));
            if (!(neighboursData.rightSide is null)) 
                result.AddRange(neighboursData.rightSide.Select(node => indexMap[node]));
            return result;
        }
        
        public static float2 ByteToDir(byte value)
        {
            var angleRad = value  * TwoPi / 255;
            return new float2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }
        
        public static float2 ByteToDir(in byte value)
        {
            var angleRad = value  * TwoPi / 255;
            return new float2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }
        
        public static byte DirToByte(float2 value)
        {
            var rad = Mathf.Atan2(value.y, value.x);
            return (byte)(rad / TwoPi * 255);
        }

        public static float2 CalcIntersection(float2 a, float2 b, float2 c, float2 d)
        {
            float m1;
            float m2;
            float b1;
            float b2;
            float x;
            float y;
            
            if (a.x == b.x)
            {
                m2 = (c.y - d.y) / (c.x - d.x);
                b2 = c.y - m2 * c.x;

                x = a.x;
                y = m2 * x + b2;
            }
            else if(c.x == d.x)
            {
                m1 = (a.y - b.y) / (a.x - b.x);
                b1 = a.y - m1 * a.x;
                
                x = c.x;
                y = m1 * x + b1;
            }
            else
            {
                m1 = (a.y - b.y) / (a.x - b.x);
                m2 = (c.y - d.y) / (c.x - d.x);

                b1 = a.y - m1 * a.x;
                b2 = c.y - m2 * c.x;

                x = (-b1 + b2) / (m1 - m2);
                y = m1 * x + b1;
            }
            return new float2(x,y);
        }

        public static float Distance(float2 a, float2 b)
        {
           return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) +
                             (a.y - b.y) * (a.y - b.y));
        }

        public static NativeArray<(float2, float2, float2)> GetExtraCurves(ECS_Node endNode, ECS_Node root, float2 target)
        {
            var resultList = new List<(float2, float2, float2)>();
            var endNodeEdges = endNode.data.GetEdges();
            var rootNodeEdges = root.data.GetEdges();
            
            if (endNodeEdges[0].x == rootNodeEdges[0].x)
            {
                var start = new float2(endNodeEdges[0].x, target.y);
                resultList.Add((start, (start + target) / 2, target));
            }
            if (endNodeEdges[1].x == rootNodeEdges[1].x)
            {
                var start = new float2(endNodeEdges[1].x, target.y);
                resultList.Add((start, (start + target) / 2, target));
            }
            if (endNodeEdges[2].y == rootNodeEdges[2].y)
            {
                var start = new float2(target.x, endNodeEdges[2].y);
                resultList.Add((start, (start + target) / 2, target));
            }
            if (endNodeEdges[3].y == rootNodeEdges[3].y)
            {
                var start = new float2(target.x, endNodeEdges[3].y);
                resultList.Add((start, (start + target) / 2, target));
            }

            var result = new NativeArray<(float2, float2, float2)>(resultList.Count, Allocator.Persistent);
            for (var i = 0; i < resultList.Count; i++)
            {
                result[i] = resultList[i];
            }
            return result;
        }
        public static int2 GetPosInFlowField(float divisionsLength, float2 pos)
        {
            return (int2) (pos / divisionsLength);
        }
        
    }
}