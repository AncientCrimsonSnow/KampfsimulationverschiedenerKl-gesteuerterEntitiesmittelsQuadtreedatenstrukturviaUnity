using Project.Development.DrawStateMachine.States.DrawPathStateMachine.DrawPerspectiveStates;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine.SelectPathVariablesStateMachine
{
    public class SelectPathNodeState : SelectPathBaseState
    {
        public override void Enter(DrawPerspectiveCustomState perspectiveCustomState)
        {
            perspectiveCustomState.EndNodeEdges.Clear();
        }

        public override void Update(DrawPerspectiveCustomState perspectiveCustomState)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                perspectiveCustomState.target = new float2(worldPos.x, worldPos.z);
                perspectiveCustomState.SetEndNode(perspectiveCustomState.Tree.GetNode(perspectiveCustomState.Tree.FindNodeData(perspectiveCustomState.target)));
                perspectiveCustomState.SwitchSelectState(perspectiveCustomState.selectShowState);
            }
        }
    }
}