using UnityEngine;

namespace Project.Development.DrawStateMachine.States
{
    public class DrawGridState : DrawBaseState
    {
        public DrawGridState(DebugDrawer drawer) : base(drawer) {}
        
        public override void Enter()
        {
            drawer.UpdateAllEdges();
        }

        public override void Update()
        {
            if(drawer.Console.DrawingMode != DrawingMode.SIMPLE_GRID)
                drawer.SwitchState();
        }

        public override void UpdateGizmo()
        {
            DrawGrid();
        }
        
        private void DrawGrid()
        {
            foreach (var edge in drawer.edgesToDraw)
            {
                Gizmos.color = drawer.gridColor;
                var p1 = new Vector3(edge.x, 0, edge.y);
                var p2 = new Vector3(edge.z, 0, edge.w);
                Gizmos.DrawLine(p1 ,p2);
            }
        }
    }
}