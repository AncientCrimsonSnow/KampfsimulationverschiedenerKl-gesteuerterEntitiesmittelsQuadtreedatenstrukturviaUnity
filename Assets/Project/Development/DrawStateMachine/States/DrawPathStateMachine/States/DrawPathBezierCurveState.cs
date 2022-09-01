using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.States
{
    public class DrawPathBezierCurveState : DrawPathBaseState
    {
        public override void Enter()
        {
        }

        public override void Update(DrawPathState pathState)
        {
            if(Console.instance.DrawingMode != DrawingMode.BEZIER_PATH)
                pathState.SwitchDrawState();
        }

        public override void UpdateGizmo(DrawPathState pathState)
        {
            DrawGrid(pathState);
            if (pathState.crrDrawPerspectiveState == pathState.perspectiveCustomState)
            {
                if(pathState.perspectiveCustomState.CrrSelectState == pathState.perspectiveCustomState.selectShowState)
                    DrawBezierCurves(pathState);
            }
            else
                DrawBezierCurves(pathState);
        }
        
        private void DrawGrid(DrawPathState pathState)
        {
            Gizmos.color = pathState.Drawer.gridColor;
            foreach (var edge in pathState.Drawer.edgesToDraw.Where(edge => !pathState.crrDrawPerspectiveState.EndNodeEdges.Contains(edge)))
            {
                var p1 = new Vector3(edge.x, 0, edge.y);
                var p2 = new Vector3(edge.z, 0, edge.w);
                Gizmos.DrawLine(p1 ,p2);
            }

            Gizmos.color = pathState.Drawer.endColor;
            foreach (var edge in pathState.crrDrawPerspectiveState.EndNodeEdges)
            {
                var p1 = new Vector3(edge.x, 0, edge.y);
                var p2 = new Vector3(edge.z, 0, edge.w);
                Gizmos.DrawLine(p1 ,p2);
            }
        }

        private void DrawBezierCurves(DrawPathState pathState)
        {
            const float sampleSize = 0.1f;
            Gizmos.color = pathState.Drawer.pathColor;
            foreach (var bezierCurve in pathState.crrDrawPerspectiveState.bezierCurves)
            {
                float2 prePoint;
                float2 postPoint;
                Vector3 p1;
                Vector3 p2;
                for (var t = sampleSize; t < 1; t += sampleSize)
                {
                    prePoint = GetPointWithT(bezierCurve.Item1,  bezierCurve.Item2, bezierCurve.Item3, t - sampleSize);
                    postPoint = GetPointWithT(bezierCurve.Item1,  bezierCurve.Item2, bezierCurve.Item3, t);
                    p1 = new Vector3(prePoint.x, 0, prePoint.y);
                    p2 = new Vector3(postPoint.x, 0, postPoint.y);
                    Gizmos.DrawLine(p1 ,p2);
                }
                prePoint = GetPointWithT(bezierCurve.Item1,  bezierCurve.Item2, bezierCurve.Item3, 1 - sampleSize);
                postPoint = GetPointWithT(bezierCurve.Item1,  bezierCurve.Item2, bezierCurve.Item3, 1);
                p1 = new Vector3(prePoint.x, 0, prePoint.y);
                p2 = new Vector3(postPoint.x, 0, postPoint.y);
                Gizmos.DrawLine(p1 ,p2);
            } 
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
    }
}