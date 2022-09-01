namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.States
{
    public abstract class DrawPathBaseState
    {

        public abstract void Enter();
        public abstract void Update(DrawPathState pathState);
        public abstract void UpdateGizmo(DrawPathState pathState);
    }
}