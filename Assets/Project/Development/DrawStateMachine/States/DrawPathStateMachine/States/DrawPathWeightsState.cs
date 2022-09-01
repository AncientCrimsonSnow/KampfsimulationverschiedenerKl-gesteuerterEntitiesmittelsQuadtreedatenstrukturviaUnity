using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.States
{
    public class DrawPathWeightsState : DrawPathBaseState
    {
        private GUIStyle _costStyle;
        
        public override void Enter()
        {
            
        }

        public override void Update(DrawPathState pathState)
        {
            if(Console.instance.DrawingMode != DrawingMode.WEIGHTS)
                pathState.SwitchDrawState();
        }

        public override void UpdateGizmo(DrawPathState pathState)
        {
            DrawGrid(pathState);
            DrawCosts(pathState);
        }
        
        private void DrawCosts(DrawPathState pathState)
        {
            _costStyle = GUI.skin.label;
            _costStyle.fontSize = 10;
            
            //h and f costs settings
            
            GUI.color = pathState.Drawer.textColor;

            foreach (var leaf in pathState.Drawer.Tree.GetAllLeafsData())
            {
                var corners = leaf.GetCorners();
                var pos = corners[1];
                var node = pathState.Drawer.Tree.GetNode(leaf);
                Handles.Label(new Vector3(pos.x, 0, pos.y), "(" +
                                                            $"{node.cost}," +
                                                            (pathState.indexMap is null ? "" : $"{pathState.indexMap[node]})"), _costStyle);
            }
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
    }
}