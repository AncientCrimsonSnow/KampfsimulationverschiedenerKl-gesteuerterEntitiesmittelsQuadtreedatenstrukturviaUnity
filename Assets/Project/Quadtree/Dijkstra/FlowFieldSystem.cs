using Project.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Console = Project.Development.Console;

namespace Project.Quadtree.AStart
{
    [UpdateInGroup(typeof(FlowFieldCalculationUpdateGrp))]
    public partial class FlowFieldSystem : SystemBase
    {
        public const float BorderThickness = 1;
        
        private float _divisionLength;
        private int _flowFieldSideLenght;
        private QuadtreeCoreSystem _quadtreeCoreSystem;
        private UnitsMovementSystem _unitsMovementSystem;
        private ECS_Quadtree _tree;
        private Console _console;
        
        public JobHandle UpdateFieldJobHandle {
            get;
            private set;
        }
        
        protected override void OnStartRunning()
        {
            _quadtreeCoreSystem = World.GetOrCreateSystem<QuadtreeCoreSystem>();
            _unitsMovementSystem = World.GetOrCreateSystem<UnitsMovementSystem>();
            _tree = _quadtreeCoreSystem.Tree;
            _console = Console.instance;
            _flowFieldSideLenght = _console.FlowFieldDivisions + 1;
            _divisionLength = (_tree.root.data.bounds.z - _tree.root.data.bounds.x) / _flowFieldSideLenght;
            
            _unitsMovementSystem.team1FlowField = new NativeArray<byte>(_flowFieldSideLenght * _flowFieldSideLenght, Allocator.Persistent);
            _unitsMovementSystem.team2FlowField = new NativeArray<byte>(_flowFieldSideLenght * _flowFieldSideLenght, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var leafs = _tree.GetAllLeafNodes();
            var (
                    team1AllPathNodes,
                    neighbourIndices, 
                    indexMap, 
                    indexCenterPointer,
                    indexEdgesPointer) = FlowFieldUtils.GetCreateFlowFieldJobData(leafs);
            var team2AllPathNodes = new NativeArray<PathNode>(team1AllPathNodes.Length, Allocator.Persistent);
            NativeArray<PathNode>.Copy(team1AllPathNodes, team2AllPathNodes);
            
            var team1EndNode = _tree.GetNode(_tree.FindNodeData(_console.Team1TargetPosition));
            var team2EndNode = _tree.GetNode(_tree.FindNodeData(_console.Team2TargetPosition));
            
            var team1PathGraph = new NativeMultiHashMap<int, int>(leafs.Count, Allocator.Persistent);
            var team2PathGraph = new NativeMultiHashMap<int, int>(leafs.Count, Allocator.Persistent);
            
            var team1CalcFlowFieldGraphJob = new CalcFlowFieldGraphJob
            {
                endNodeIndex = indexMap[team1EndNode],
                allNodes = team1AllPathNodes,
                neighbourIndicesPointer = neighbourIndices,
                graph = team1PathGraph
            };
            var team1CalcFlowFieldGraphJobHandle = team1CalcFlowFieldGraphJob.Schedule();
            
            team1AllPathNodes.Dispose(team1CalcFlowFieldGraphJobHandle);
            
            var team2CalcFlowFieldGraphJob = new CalcFlowFieldGraphJob
            {
                endNodeIndex = indexMap[team2EndNode],
                allNodes = team2AllPathNodes,
                neighbourIndicesPointer = neighbourIndices,
                graph = team2PathGraph
            };
            var team2CalcFlowFieldGraphJobHandle = team2CalcFlowFieldGraphJob.Schedule();
            
            team2AllPathNodes.Dispose(team2CalcFlowFieldGraphJobHandle);
            
            neighbourIndices.Dispose(JobHandle.CombineDependencies(team1CalcFlowFieldGraphJobHandle, team2CalcFlowFieldGraphJobHandle));
            
            var team1BezierCurves = new NativeList<(float2, float2, float2)>(leafs.Count, Allocator.Persistent);
            var team2BezierCurves = new NativeList<(float2, float2, float2)>(leafs.Count, Allocator.Persistent);

            var team1ExtraPaths = FlowFieldUtils.GetExtraCurves(team1EndNode, _tree.root, _console.Team1TargetPosition);
            var team2ExtraPaths = FlowFieldUtils.GetExtraCurves(team2EndNode, _tree.root, _console.Team2TargetPosition);
            
            var team1CreateBezierCurveJob = new CreateBezierCurvesJob
            {
                graph = team1PathGraph,
                indicesCenterPointer = indexCenterPointer,
                indexEdgesPointer = indexEdgesPointer,
                bezierCurves = team1BezierCurves,
                extraPaths = team1ExtraPaths,
                endNodeIndex = indexMap[team1EndNode],
                target = _console.Team1TargetPosition,
            };
            var team1CreateBezierCurveJobHandle = team1CreateBezierCurveJob.Schedule(team1CalcFlowFieldGraphJobHandle);

            team1PathGraph.Dispose(team1CreateBezierCurveJobHandle);
            team1ExtraPaths.Dispose(team1CreateBezierCurveJobHandle);
            
            var team2CreateBezierCurveJob = new CreateBezierCurvesJob
            {
                graph = team2PathGraph,
                indicesCenterPointer = indexCenterPointer,
                indexEdgesPointer = indexEdgesPointer,
                bezierCurves = team2BezierCurves,
                extraPaths = team2ExtraPaths,
                endNodeIndex = indexMap[team2EndNode],
                target = _console.Team2TargetPosition,
            };
            var team2CreateBezierCurveJobHandle = team2CreateBezierCurveJob.Schedule(team2CalcFlowFieldGraphJobHandle);
            
            team2PathGraph.Dispose(team2CreateBezierCurveJobHandle);
            team2ExtraPaths.Dispose(team2CreateBezierCurveJobHandle);
            
            indexCenterPointer.Dispose(JobHandle.CombineDependencies(team1CreateBezierCurveJobHandle, team2CreateBezierCurveJobHandle));
            indexEdgesPointer.Dispose(JobHandle.CombineDependencies(team1CreateBezierCurveJobHandle, team2CreateBezierCurveJobHandle));
            
            var team1FlowField = Native2dArrayUtils.CreateArray<byte>(_flowFieldSideLenght, _flowFieldSideLenght, Allocator.Persistent);
            var team2FlowField = Native2dArrayUtils.CreateArray<byte>(_flowFieldSideLenght, _flowFieldSideLenght, Allocator.Persistent);
            
            var team1FillFlowFieldJob = new FillFlowFieldJob
            {
                flowField = team1FlowField,
                divisionLength = _divisionLength,
                flowFieldWidth = _flowFieldSideLenght,
                bezierCurves = team1BezierCurves,
                target = _console.Team1TargetPosition,
            };
            var team1FillFlowFieldJobHandle = team1FillFlowFieldJob.Schedule(team1FlowField.Length, _flowFieldSideLenght, team1CreateBezierCurveJobHandle);
            
            team1BezierCurves.Dispose(team1FillFlowFieldJobHandle);
            
            var team2FillFlowFieldJob = new FillFlowFieldJob
            {
                flowField = team2FlowField,
                divisionLength = _divisionLength,
                flowFieldWidth = _flowFieldSideLenght,
                bezierCurves = team2BezierCurves,
                target = _console.Team2TargetPosition,
            };
            var team2FillFlowFieldJobHandle = team2FillFlowFieldJob.Schedule(team2FlowField.Length, _flowFieldSideLenght, team2CreateBezierCurveJobHandle);

            team2BezierCurves.Dispose(team2FillFlowFieldJobHandle);
            
            var team1SmoothFlowField = Native2dArrayUtils.CreateArray<byte>(_flowFieldSideLenght, _flowFieldSideLenght, Allocator.Persistent);
            var team2SmoothFlowField = Native2dArrayUtils.CreateArray<byte>(_flowFieldSideLenght, _flowFieldSideLenght, Allocator.Persistent);
            
            var team1SmoothFlowFieldJob = new SmoothFlowFieldJob
            {
                flowFieldWidth = _flowFieldSideLenght,
                unSmoothedFlowField = team1FlowField,
                smoothedFlowField = team1SmoothFlowField,
            };
            var team1SmoothFlowFieldJobHandle = team1SmoothFlowFieldJob.Schedule(team1SmoothFlowField.Length, _flowFieldSideLenght, team1FillFlowFieldJobHandle);

            team1FlowField.Dispose(team1SmoothFlowFieldJobHandle);
            
            var team2SmoothFlowFieldJob = new SmoothFlowFieldJob
            {
                flowFieldWidth = _flowFieldSideLenght,
                unSmoothedFlowField = team2FlowField,
                smoothedFlowField = team2SmoothFlowField,
            };
            var team2SmoothFlowFieldJobHandle = team2SmoothFlowFieldJob.Schedule(team2SmoothFlowField.Length, _flowFieldSideLenght, team2FillFlowFieldJobHandle);

            team2FlowField.Dispose(team2SmoothFlowFieldJobHandle);

            _unitsMovementSystem.MoveJobHandle.Complete();
            UpdateFieldJobHandle = new CopyFlowField
            {
                team1FlowFieldSrc = team1SmoothFlowField,
                team2FlowFieldSrc = team2SmoothFlowField,
                team1FlowFieldOut = _unitsMovementSystem.team1FlowField,
                team2FlowFieldOut = _unitsMovementSystem.team2FlowField,
            }.Schedule(JobHandle.CombineDependencies(team1SmoothFlowFieldJobHandle, team2SmoothFlowFieldJobHandle));

            team1SmoothFlowField.Dispose(UpdateFieldJobHandle);
            team2SmoothFlowField.Dispose(UpdateFieldJobHandle);
        }
    }


    public class FlowFieldCalculationUpdateGrp : ComponentSystemGroup
    {
        public FlowFieldCalculationUpdateGrp()
        {
            RateManager = new RateUtils.VariableRateManager(500, false);
        }
    }
    
    [BurstCompile]
    public struct CalcFlowFieldGraphJob : IJob
    {
        [ReadOnly]
        public int endNodeIndex;
        public NativeArray<PathNode> allNodes;
        
        [ReadOnly]
        public NativeMultiHashMap<int, int> neighbourIndicesPointer;

        public NativeMultiHashMap<int, int> graph;

        public void Execute()
        {
            var graphHelper = new NativeMultiHashMap<int, int>(allNodes.Length, Allocator.Temp);
            SetCosts(graphHelper);
            SetUpGraph(graphHelper);
        }
        
        private void SetCosts(NativeMultiHashMap<int, int> graphHelper)
        {
            for (var i = 0; i != allNodes.Length; i++)
            {
                var node = allNodes[i];
                node.cost = int.MaxValue;
                allNodes[i] = node;
            }

            var startNode = allNodes[endNodeIndex];
            startNode.cost = 0;
            allNodes[endNodeIndex] = startNode;
            CheckNeighbours(startNode, graphHelper);
        }
        
        private void CheckNeighbours(PathNode crrNode, NativeMultiHashMap<int, int> graphHelper)
        {
            var tentativeGCost = crrNode.cost + 1;

            var i = crrNode.nodeIndexInLeafs;
            var neighboursIndices = neighbourIndicesPointer.GetValuesForKey(i);
            foreach (var neighboursIndex in neighboursIndices)
            {
                var neighbourNode = allNodes[neighboursIndex];
                if (neighbourNode.cost > tentativeGCost)
                {
                    graphHelper.Remove(neighboursIndex);
                    graphHelper.Add(neighboursIndex, crrNode.nodeIndexInLeafs);
                    neighbourNode.cost = tentativeGCost;
                    allNodes[neighboursIndex] = neighbourNode;
                    CheckNeighbours(neighbourNode, graphHelper);
                }
                else if(neighbourNode.cost == tentativeGCost)
                {
                    graphHelper.Add(neighboursIndex, crrNode.nodeIndexInLeafs);
                }
            }
        }

        private void SetUpGraph(NativeMultiHashMap<int, int> graphHelper)
        {
            foreach (var node in allNodes)
            {
                if(!graphHelper.ContainsKey(node.nodeIndexInLeafs))
                    continue;
                var cameFromNodes = graphHelper.GetValuesForKey(node.nodeIndexInLeafs);
                while (cameFromNodes.MoveNext())
                {
                    graph.Add(node.nodeIndexInLeafs, cameFromNodes.Current);
                }
            }
        }
    }

    [BurstCompile]
    public struct CreateBezierCurvesJob : IJob
    {
        [ReadOnly]
        public NativeMultiHashMap<int, int> graph;
        [ReadOnly]
        public NativeHashMap<int, float2> indicesCenterPointer;
        [ReadOnly]
        public NativeMultiHashMap<int, float4> indexEdgesPointer;
        [ReadOnly]
        public NativeArray<(float2, float2, float2)> extraPaths;
        [ReadOnly]
        public int endNodeIndex;
        [ReadOnly]
        public float2 target;
        
        public NativeList<(float2, float2, float2)> bezierCurves;
        public void Execute()
        {
            CalcBezierCurvePoints();
        }

        private void CalcBezierCurvePoints()
        {
            var allOutComingPoints =  new NativeMultiHashMap<int, float2>(graph.Count(), Allocator.Temp);
            var allInComingPoints =  new NativeMultiHashMap<int, float2>(graph.Count(), Allocator.Temp);
            
            foreach (var edgeIndices in graph)
            {
                var p1 = indicesCenterPointer[edgeIndices.Key];
                var p2 = edgeIndices.Value == endNodeIndex ? target : indicesCenterPointer[edgeIndices.Value];
                
                var edgeDistance = FlowFieldUtils.Distance(p1, p2);
                
                var plumbPoint = FindPlumbPoint(edgeIndices.Key, edgeDistance, p1, p2);
                
                allOutComingPoints.Add(edgeIndices.Key, plumbPoint);
                allInComingPoints.Add(edgeIndices.Value, plumbPoint);
            }

            foreach (var indexCenterPointer in indicesCenterPointer)
            {
                var index = indexCenterPointer.Key;
                var center = indexCenterPointer.Value;
                if (allInComingPoints.ContainsKey(index))
                {
                    foreach (var inComingPoint in allInComingPoints.GetValuesForKey(index))
                    {
                        if (allOutComingPoints.ContainsKey(index))
                        {
                            foreach (var outComingPoint in allOutComingPoints.GetValuesForKey(index))
                            {
                                bezierCurves.Add((inComingPoint, center, outComingPoint));
                            }
                        }
                        else
                        {
                            bezierCurves.Add((inComingPoint, (inComingPoint + target) / 2, target));
                        }
                    }
                }
                else
                {
                    foreach (var outComingPoint in allOutComingPoints.GetValuesForKey(index))
                    {
                            bezierCurves.Add((center, (center + outComingPoint) / 2, outComingPoint));
                    }
                }
            }
            
            foreach (var extraPath in extraPaths)
            {
                bezierCurves.Add(extraPath);
            }
            
        }

        private float2 FindPlumbPoint(int index, float edgeDistance, float2 p1, float2 p2)
        {
            foreach (var edge in indexEdgesPointer.GetValuesForKey(index))
            {
                var plumbPoint = FlowFieldUtils.CalcIntersection(p1, p2, edge.xy, edge.zw);
                var distance1 = FlowFieldUtils.Distance(plumbPoint, p1);
                var distance2 = FlowFieldUtils.Distance(plumbPoint, p2);
                if (distance1 < edgeDistance && distance2 < edgeDistance)
                {
                    return plumbPoint;
                }
            }
            return float2.zero;
        }
    }
    
    [BurstCompile]
    public struct FillFlowFieldJob : IJobParallelFor
    {
        public NativeArray<byte> flowField;

        [ReadOnly]
        public int flowFieldWidth;
        [ReadOnly]
        public float divisionLength;
        [ReadOnly]
        public NativeList<(float2, float2, float2)> bezierCurves;
        [ReadOnly]
        public float2 target;
        
        public void Execute(int index)
        {
            var coordinate = Native2dArrayUtils.FlatIndexToCoordinates(index, flowFieldWidth);
            var cellCenterPos = GetFieldPos(coordinate);
            if (coordinate.x < FlowFieldSystem.BorderThickness ||
                coordinate.y < FlowFieldSystem.BorderThickness ||
                coordinate.x >= flowFieldWidth - FlowFieldSystem.BorderThickness ||
                coordinate.y >= flowFieldWidth - FlowFieldSystem.BorderThickness
               )
            {
                var dirToTarget = target - cellCenterPos;
                flowField[index] = FlowFieldUtils.DirToByte(dirToTarget);
                return;
            }

            var shortestDistance = float.MaxValue;
            var shortestDistanceTValue = float.MaxValue;
            (float2, float2, float2) shortestDistanceBezierCurve = default;
            
            foreach (var bezierCurve in bezierCurves)
            {
                var (distance, t) = GetTValueOfShortestPos(bezierCurve.Item1, bezierCurve.Item2, bezierCurve.Item3, cellCenterPos);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestDistanceTValue = t;
                    shortestDistanceBezierCurve = bezierCurve;
                }
            }

            var shortestDistanceValue = GetDirOfBezierPoint(shortestDistanceBezierCurve.Item1, shortestDistanceBezierCurve.Item2, shortestDistanceBezierCurve.Item3, shortestDistanceTValue);
            flowField[index] = FlowFieldUtils.DirToByte(shortestDistanceValue);
        }

        private (float, float) GetTValueOfShortestPos(float2 p1, float2 p2, float2 p3, float2 pos)
        {
            const float sampleSize = 0.05f;

            var closestDistance = float.MaxValue;
            var closestBezierPointTValue = float.MaxValue;
            
            for (var t = 0f; t <= 1; t += sampleSize)
            {
                var p = GetPointWithT(p1, p2, p3, t);
                var distance = FlowFieldUtils.Distance(pos, p);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBezierPointTValue = t;
                }
            }
            
            return (closestDistance, closestBezierPointTValue);
        }

        private float2 GetDirOfBezierPoint(float2 p1, float2 p2, float2 p3, float t)
        {
            const float sampleRange = 0.1f;
            var prePoint = GetPointWithT(p1, p2, p3, t - sampleRange);
            var postPoint = GetPointWithT(p1, p2, p3, t + sampleRange);
            return postPoint - prePoint;
        }

        private float2 GetPointWithT(float2 p1, float2 p2, float2 p3, float t)
        {
            var p12 = Lerp(p1, p2, t);
            var p23 = Lerp(p2, p3, t);
            return Lerp(p12, p23, t);
        }

        private float2 Lerp(float2 a, float2 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new float2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        private float2 GetFieldPos(float2 coordinate)
        {
            return new float2(
                coordinate.x * divisionLength + divisionLength / 2,
                coordinate.y * divisionLength + divisionLength / 2);
        }
    }

    [BurstCompile]
    public struct SmoothFlowFieldJob : IJobParallelFor
    {
        [ReadOnly]
        public int flowFieldWidth;
        [ReadOnly]
        public NativeArray<byte> unSmoothedFlowField;
        
        public NativeArray<byte> smoothedFlowField;
        
        public void Execute(int index)
        {
            var coordinate = Native2dArrayUtils.FlatIndexToCoordinates(index, flowFieldWidth);
            if (coordinate.x < FlowFieldSystem.BorderThickness ||
                coordinate.y < FlowFieldSystem.BorderThickness ||
                coordinate.x >= flowFieldWidth - FlowFieldSystem.BorderThickness ||
                coordinate.y >= flowFieldWidth - FlowFieldSystem.BorderThickness
               )
            {
                smoothedFlowField[index] = unSmoothedFlowField[index];
                return;
            }
            
            var neighbours = GetNeighbourValues(coordinate);
            var sum = FlowFieldUtils.ByteToDir(unSmoothedFlowField[index]);
            foreach (var neighbour in neighbours)
            {
                sum += neighbour;
            }
            smoothedFlowField[index] = FlowFieldUtils.DirToByte(sum/9);
        }

        private NativeArray<float2> GetNeighbourValues(int2 center)
        {
            var result = new NativeArray<float2>(8, Allocator.Temp);
            result[0] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(-1, 1), flowFieldWidth)]);
            result[1] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(0, 1), flowFieldWidth)]);
            result[2] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(1, 1), flowFieldWidth)]);
            result[3] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(-1, 0), flowFieldWidth)]);
            result[4] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(1, 0), flowFieldWidth)]);
            result[5] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(-1, -1), flowFieldWidth)]);
            result[6] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(0, -1), flowFieldWidth)]);
            result[7] = FlowFieldUtils.ByteToDir(unSmoothedFlowField[Native2dArrayUtils.CoordinatesToFlatIndex(center + new int2(1, -1), flowFieldWidth)]);
            return result;
        }
    }

    public struct CopyFlowField : IJob
    {
        [ReadOnly]
        public NativeArray<byte> team1FlowFieldSrc;
        [ReadOnly]
        public NativeArray<byte> team2FlowFieldSrc;
        
        public NativeArray<byte> team1FlowFieldOut;
        public NativeArray<byte> team2FlowFieldOut;
        public void Execute()
        {
            NativeArray<byte>.Copy(team1FlowFieldSrc, team1FlowFieldOut);
            NativeArray<byte>.Copy(team2FlowFieldSrc, team2FlowFieldOut);
        }
    }
}