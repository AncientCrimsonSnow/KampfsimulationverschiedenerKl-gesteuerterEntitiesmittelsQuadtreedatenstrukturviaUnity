using System;
using System.Collections.Generic;
using Project.Quadtree.AStart;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.States
{
    public class DrawFlowFieldState : DrawPathBaseState, IDisposable
    {
        public NativeArray<byte> flowField;
        
        public override void Enter()
        {
        }

        public override void Update(DrawPathState pathState)
        {
            if (Console.instance.DrawingMode != DrawingMode.FLOW_FIELD)
            {
                pathState.SwitchDrawState();
            }
        }

        public override void UpdateGizmo(DrawPathState pathState)
        {
            if (pathState.crrDrawPerspectiveState == pathState.perspectiveCustomState)
            {
                if(pathState.perspectiveCustomState.CrrSelectState == pathState.perspectiveCustomState.selectShowState)
                    DrawFlowField(pathState);
            }
            else
                DrawFlowField(pathState);
        }
        
        private void DrawFlowField(DrawPathState context)
        {
            var divisions = Console.instance.FlowFieldDivisions + 1;
            var segmentLength = (context.Drawer.Tree.root.data.bounds.z - context.Drawer.Tree.root.data.bounds.x) / divisions;
            var edgesToDraw = new HashSet<float4>();
            var startCoordinate = context.Drawer.Tree.root.data.bounds.xy;
            
            for(var i = 0; i < flowField.Length; i++)
            {
                var arrayCoordinate = Native2dArrayUtils.FlatIndexToCoordinates(i, divisions);
                var value = flowField[i];
                var dir = FlowFieldUtils.ByteToDir(value);
                dir *= segmentLength * 0.8f;
                var pos1ToDraw = startCoordinate + (float2)arrayCoordinate * segmentLength;
                var center = pos1ToDraw + new float2(segmentLength, segmentLength) / 2;
                Gizmos.color = context.Drawer.pathColor;
                var p1Float = center + dir / -2;
                var p2Float = center + dir / 2;

                var p1 = new Vector3(p1Float.x, 0, p1Float.y);
                var p2 = new Vector3(p2Float.x, 0, p2Float.y);
                Gizmos.DrawLine(p1, p2);
                
                var arrowDistance = segmentLength / 7;
                var arrowHelper = (p1 - p2).normalized * arrowDistance;
                var arrowP1 = Quaternion.Euler(0, 45, 0) * arrowHelper + p2;
                var arrowP2 = Quaternion.Euler(0, -45, 0) * arrowHelper + p2;
                Gizmos.DrawLine(p2 ,arrowP1);
                Gizmos.DrawLine(p2 ,arrowP2);
                
                var square = new float4(pos1ToDraw.x, pos1ToDraw.y, 
                    pos1ToDraw.x + segmentLength,
                    pos1ToDraw.y + segmentLength);
                
                edgesToDraw.Add(square.xyxw);
                edgesToDraw.Add(square.zyzw);
                edgesToDraw.Add(square.xwzw);
                edgesToDraw.Add(square.xyzy);
            }
            foreach (var edge in edgesToDraw)
            {
                Gizmos.color = context.Drawer.gridColor;
                var p1 = new Vector3(edge.x, 0, edge.y);
                var p2 = new Vector3(edge.z, 0, edge.w);
                Gizmos.DrawLine(p1 ,p2);
            }
        }

        public void Dispose()
        {
            flowField.Dispose();
        }
    }
}