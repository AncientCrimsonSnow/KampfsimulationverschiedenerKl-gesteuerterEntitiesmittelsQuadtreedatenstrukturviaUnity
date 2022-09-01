using Project.Development.DrawStateMachine.States.DrawPathStateMachine.SelectPathVariablesStateMachine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.DrawPerspectiveStates
{
    public class DrawPerspectiveCustomState : DrawPerspectiveBaseState
    {
        public SelectPathBaseState CrrSelectState { get; private set; }

        public readonly SelectPathNodeState selectNodeState = new SelectPathNodeState();
        public readonly SelectShowState selectShowState = new SelectShowState();
        

        public override void Enter()
        {
            SwitchSelectState(selectNodeState);
        }

        public override void Update()
        {
            if(Console.instance.DrawingPerspective != DrawingPerspective.CUSTOM)
                pathState.SwitchPerspectiveState();
            
            CrrSelectState.Update(this);
        }
        
        public void SwitchSelectState(SelectPathBaseState state)
        {
            CrrSelectState = state;
            CrrSelectState.Enter(this);
        }

        public DrawPerspectiveCustomState(DebugDrawer drawer, DrawPathState pathState) : base(drawer, pathState)
        {
        }
    }
}