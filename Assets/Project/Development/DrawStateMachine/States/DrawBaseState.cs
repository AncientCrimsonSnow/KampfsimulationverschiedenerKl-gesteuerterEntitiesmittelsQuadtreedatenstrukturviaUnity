namespace Project.Development.DrawStateMachine.States
{
    public abstract class DrawBaseState
    {
        protected DebugDrawer drawer;

        public DebugDrawer Drawer => drawer;

        protected DrawBaseState(DebugDrawer drawer)
        {
            this.drawer = drawer;
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void UpdateGizmo();
    }
}