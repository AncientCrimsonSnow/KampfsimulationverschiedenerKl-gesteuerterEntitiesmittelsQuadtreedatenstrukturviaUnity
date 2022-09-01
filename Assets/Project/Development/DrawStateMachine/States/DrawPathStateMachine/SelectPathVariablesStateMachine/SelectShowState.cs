using Project.Development.DrawStateMachine.States.DrawPathStateMachine.DrawPerspectiveStates;
using Project.Quadtree;
using Project.Quadtree.AStart;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.SelectPathVariablesStateMachine
{
    public class SelectShowState : SelectPathBaseState
    {
        public override void Enter(DrawPerspectiveCustomState perspectiveCustomState)
        {
            var flowFieldSideLenght = Console.instance.FlowFieldDivisions + 1;
            var leafs = perspectiveCustomState.Tree.GetAllLeafNodes();
            var flowField = Native2dArrayUtils.CreateArray<byte>(flowFieldSideLenght, flowFieldSideLenght, Allocator.Persistent);
            var divisionLength = (perspectiveCustomState.Tree.root.data.bounds.z - perspectiveCustomState.Tree.root.data.bounds.x) / flowFieldSideLenght;
            
            var (
                    allPathNodes,
                    neighbourIndices, 
                    indexMap, 
                    indexCenterPointer,
                    indexEdgePointer) 
                = FlowFieldUtils.GetCreateFlowFieldJobData(leafs);
            
            if (perspectiveCustomState.pathGraph.IsCreated)
                perspectiveCustomState.pathGraph.Dispose();
            perspectiveCustomState.pathGraph = new NativeMultiHashMap<int, int>(leafs.Count, Allocator.Persistent);
            
            if (perspectiveCustomState.Leafs.IsCreated)
                perspectiveCustomState.Leafs.Dispose();
            perspectiveCustomState.Leafs = allPathNodes;
            
            perspectiveCustomState.IndexMap = indexMap;
            
            CallCalcFlowFieldGraphJob(
                allPathNodes, 
                indexMap[perspectiveCustomState.endNode],
                neighbourIndices, 
                perspectiveCustomState.pathGraph);
            
            if (perspectiveCustomState.bezierCurves.IsCreated)
                perspectiveCustomState.bezierCurves.Dispose();
            perspectiveCustomState.bezierCurves = new NativeList<(float2, float2, float2)>(perspectiveCustomState.pathGraph.Count(), Allocator.Persistent);

            CallCreateBezierCurveJob(
                perspectiveCustomState.pathGraph, 
                indexCenterPointer, 
                indexEdgePointer, 
                perspectiveCustomState.bezierCurves, 
                FlowFieldUtils.GetExtraCurves(perspectiveCustomState.endNode, perspectiveCustomState.Tree.root, perspectiveCustomState.target), 
                indexMap[perspectiveCustomState.endNode], 
                perspectiveCustomState.target);
            CallFillFlowFieldJob(
                flowField, 
                divisionLength, 
                perspectiveCustomState.bezierCurves, 
                perspectiveCustomState.target);

            var smoothedFlowField = new NativeArray<byte>(flowField.Length, Allocator.Persistent);
            SmoothFlowField(flowField, smoothedFlowField);
            
            foreach (var pathNode in perspectiveCustomState.Leafs)
            {
                perspectiveCustomState.Tree.AllNodes[pathNode.nodeIndexInTree].cost = pathNode.cost;
            }
            
            if (perspectiveCustomState.FlowField.IsCreated)
                perspectiveCustomState.FlowField.Dispose();
            perspectiveCustomState.FlowField = smoothedFlowField;
        }

        public override void Update(DrawPerspectiveCustomState perspectiveCustomState)
        {
            if (Input.GetMouseButtonDown(1))
            {
                perspectiveCustomState.SwitchSelectState(perspectiveCustomState.selectNodeState);
            }
        }

        private void CallCalcFlowFieldGraphJob(NativeArray<PathNode> leafs, int endNodeIndex, NativeMultiHashMap<int, int> neighbourIndices, NativeMultiHashMap<int, int> pathGraph)
        {
            var calcFlowFieldGraphJob = new CalcFlowFieldGraphJob
            {
                endNodeIndex = endNodeIndex,
                allNodes = leafs,
                neighbourIndicesPointer = neighbourIndices,
                graph = pathGraph
            };
            calcFlowFieldGraphJob.Run();
            neighbourIndices.Dispose();
        }

        private void CallCreateBezierCurveJob(NativeMultiHashMap<int, int> pathGraph, NativeHashMap<int, float2> indexCenterPointer, NativeMultiHashMap<int, float4> indexEdgesPointer, NativeList<(float2, float2, float2)>  bezierCurves, NativeArray<(float2, float2, float2)> extraPaths, int endNodeIndex, float2 target)
        {
            var createBezierCurveJob = new CreateBezierCurvesJob
            {
                graph = pathGraph,
                indicesCenterPointer = indexCenterPointer,
                indexEdgesPointer = indexEdgesPointer,
                bezierCurves = bezierCurves,
                extraPaths = extraPaths,
                endNodeIndex = endNodeIndex,
                target = target,
            };
            createBezierCurveJob.Run();
            indexEdgesPointer.Dispose();
            indexCenterPointer.Dispose();
            extraPaths.Dispose();
        }

        private void CallFillFlowFieldJob(NativeArray<byte> flowField, float divisionLength, NativeList<(float2, float2, float2)>  bezierCurves, float2 target)
        {
            var fillFlowFieldJob = new FillFlowFieldJob
            {
                flowField = flowField,
                divisionLength = divisionLength,
                flowFieldWidth = Console.instance.FlowFieldDivisions + 1,
                bezierCurves = bezierCurves,
                target = target,
            };
            var jobHandle = fillFlowFieldJob.Schedule(flowField.Length, Console.instance.FlowFieldDivisions + 1);
            jobHandle.Complete();
        }
        
        private void SmoothFlowField(NativeArray<byte> unSmoothedFlowField, NativeArray<byte> smoothedFlowField)
        {
            var smoothFlowFieldJob = new SmoothFlowFieldJob
            {
                flowFieldWidth = Console.instance.FlowFieldDivisions + 1,
                unSmoothedFlowField = unSmoothedFlowField,
                smoothedFlowField = smoothedFlowField
            };
            var jobHandle = smoothFlowFieldJob.Schedule(smoothedFlowField.Length, Console.instance.FlowFieldDivisions + 1);
            jobHandle.Complete();
            unSmoothedFlowField.Dispose();
        }
    }
}