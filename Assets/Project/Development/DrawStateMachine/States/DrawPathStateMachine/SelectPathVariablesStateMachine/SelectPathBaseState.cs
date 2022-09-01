
using Project.Development.DrawStateMachine.States.DrawPathStateMachine.DrawPerspectiveStates;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.SelectPathVariablesStateMachine
{
    public abstract class SelectPathBaseState
    {
        public abstract void Enter(DrawPerspectiveCustomState perspectiveCustomState);
        public abstract void Update(DrawPerspectiveCustomState perspectiveCustomState);
        
    }
}