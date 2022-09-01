namespace Project.Development.DrawStateMachine.States
{
    public class DrawNothingState : DrawBaseState
    {
        public DrawNothingState(DebugDrawer drawer) : base(drawer) { }

        public override void Enter()
        {
            drawer.UpdateAllEdges();
        }

        public override void Update()
        {
            if(drawer.Console.DrawingMode != DrawingMode.NONE)
                drawer.SwitchState();
        }

        public override void UpdateGizmo()
        {
            
        }
    }
}