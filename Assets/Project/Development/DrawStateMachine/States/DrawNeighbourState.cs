using System.Collections.Generic;
using System.Linq;
using Project.Quadtree;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States
{
    public class DrawNeighbourState : DrawBaseState
    {
        private readonly HashSet<float4> _neighbourhoodEdges = new HashSet<float4>();

        public DrawNeighbourState(DebugDrawer drawer) : base(drawer) {}
        
        public override void Enter()
        {
            drawer.UpdateAllEdges();
        }

        public override void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var pos = new float2(worldPos.x, worldPos.z);
                SetHighlightedLines(GetPointedNode(pos));
            }

            if (Input.GetMouseButtonDown(1))
            {
                _neighbourhoodEdges.Clear();
            }
            
            if(drawer.Console.DrawingMode != DrawingMode.NEIGHBOUR_GRID)
                drawer.SwitchState();
        }

        public override void UpdateGizmo()
        {
            DrawGrid();
        }

        private void SetHighlightedLines(ECS_Node node)
        {
            _neighbourhoodEdges.Clear();
            var highlightedNodes = new List<ECS_Node> { node };

            if(!(node.neighbourNodes.leftSide is null))
                highlightedNodes.AddRange(node.neighbourNodes.leftSide);
            if(!(node.neighbourNodes.topSide is null))
                highlightedNodes.AddRange(node.neighbourNodes.topSide);
            if(!(node.neighbourNodes.rightSide is null))
                highlightedNodes.AddRange(node.neighbourNodes.rightSide);
            if(!(node.neighbourNodes.botSide is null))
                highlightedNodes.AddRange(node.neighbourNodes.botSide);
            
            foreach (var edge in highlightedNodes.SelectMany(highlightedNode => highlightedNode.data.GetEdges()))
            {
                _neighbourhoodEdges.Add(edge);
            }
        }
        
        private void DrawGrid()
        {
            foreach (var edge in drawer.edgesToDraw.Where(edge => !_neighbourhoodEdges.Contains(edge)))
            {
                Gizmos.color = drawer.gridColor;
                var p1 = new Vector3(edge.x, 0, edge.y);
                var p2 = new Vector3(edge.z, 0, edge.w);
                Gizmos.DrawLine(p1 ,p2);
            }
            foreach (var edge in _neighbourhoodEdges)
            {
                Gizmos.color = drawer.neighbourColor;
                var p1 = new Vector3(edge.x, 0, edge.y);
                var p2 = new Vector3(edge.z, 0, edge.w);
                Gizmos.DrawLine(p1 ,p2);
            }
        }
        
        private ECS_Node GetPointedNode(float2 target)
        {
            return drawer.Tree.GetNode(drawer.Tree.FindNodeData(target));
        }
    }
}