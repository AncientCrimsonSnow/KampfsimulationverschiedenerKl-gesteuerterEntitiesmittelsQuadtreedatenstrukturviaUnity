using System.Linq;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.States
{
    public class DrawSimplePathState : DrawPathBaseState
    {
        public override void Enter()
        {
            
        }

        public override void Update(DrawPathState pathState)
        {
            if(Console.instance.DrawingMode != DrawingMode.SIMPLE_PATH)
                pathState.SwitchDrawState();
        }

        public override void UpdateGizmo(DrawPathState pathState)
        {
            DrawGrid(pathState);
            if (pathState.crrDrawPerspectiveState == pathState.perspectiveCustomState)
            {
                if(pathState.perspectiveCustomState.CrrSelectState == pathState.perspectiveCustomState.selectShowState)
                    DrawSimplePathTree(pathState);
            }
            else
                DrawSimplePathTree(pathState);
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
        
        private void DrawSimplePathTree(DrawPathState pathState)
        {
            var pathTree = pathState.crrDrawPerspectiveState.GetPathTree(pathState.leafs);
            if (pathTree == null)
                return;
            
            Gizmos.color = pathState.Drawer.pathColor; 
            foreach (var node in pathTree)
            {
                var p1 = new Vector3(node.Key.x, 0, node.Key.y);
                foreach (var p2 in node.Value.Select(branch => new Vector3(branch.x, 0, branch.y)))
                {
                    Gizmos.DrawLine(p1 ,p2);
                    var center = (p1 + p2)/2;
                    var lineVector = p1 - p2;
                    var ortho1 = Quaternion.Euler(0, 45, 0) * lineVector.normalized + center;
                    Gizmos.DrawLine(center , ortho1);
                    var ortho2 = Quaternion.Euler(0, -45, 0) * lineVector.normalized + center;
                    Gizmos.DrawLine(center ,ortho2);
                }
            }
        }
    }
}