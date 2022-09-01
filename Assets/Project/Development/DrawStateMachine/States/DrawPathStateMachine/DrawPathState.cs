using System;
using System.Collections.Generic;
using Project.Development.DrawStateMachine.States.DrawPathStateMachine.DrawPerspectiveStates;
using Project.Development.DrawStateMachine.States.DrawPathStateMachine.States;
using Project.Quadtree;
using Unity.Collections;
namespace Project.Development.DrawStateMachine.States.DrawPathStateMachine
{
    public class DrawPathState : DrawBaseState, IDisposable
    {
        public NativeArray<PathNode> leafs;
        
        public Dictionary<ECS_Node, int> indexMap;
        
        private DrawPathBaseState _crrDrawState;

        public readonly DrawSimplePathState simplePathState = new DrawSimplePathState();
        public readonly DrawPathWeightsState pathWeightsState = new DrawPathWeightsState();
        public readonly DrawPathBezierCurveState bezierCurveState = new DrawPathBezierCurveState();
        public readonly DrawFlowFieldState flowFieldState = new DrawFlowFieldState();

        public DrawPerspectiveBaseState crrDrawPerspectiveState;

        public readonly DrawPerspectiveCustomState perspectiveCustomState;

        public DrawPathState(DebugDrawer drawer) : base(drawer)
        {
            perspectiveCustomState = new DrawPerspectiveCustomState(drawer, this);
        }
        
        public override void Enter()
        {
            drawer.UpdateAllEdges();
            SwitchDrawState();
            SwitchPerspectiveState();
        }

        public override void Update()
        {
            _crrDrawState.Update(this);
            crrDrawPerspectiveState.Update();
        }

        public override void UpdateGizmo()
        {
            _crrDrawState.UpdateGizmo(this);
        }

        public void SwitchDrawState()
        {
            switch (Console.instance.DrawingMode)
            {
                case DrawingMode.WEIGHTS:
                    _crrDrawState = pathWeightsState;
                    break;
                case DrawingMode.SIMPLE_PATH:
                    _crrDrawState = simplePathState;
                    break;
                case DrawingMode.FLOW_FIELD:
                    _crrDrawState = flowFieldState;
                    break;
                case DrawingMode.BEZIER_PATH:
                    _crrDrawState = bezierCurveState;
                    break;
                case DrawingMode.NONE:
                case DrawingMode.SIMPLE_GRID:
                case DrawingMode.NEIGHBOUR_GRID:
                default:
                    drawer.SwitchState();
                    return;
            }
            _crrDrawState.Enter();
        }
        
        public void SwitchPerspectiveState()
        {
            switch (Console.instance.DrawingPerspective)
            {
                case DrawingPerspective.CUSTOM:
                    crrDrawPerspectiveState = perspectiveCustomState;
                    break;
            }
            crrDrawPerspectiveState.Enter();
        }
        
        
        public void Dispose()
        {
            if(leafs.IsCreated)
                leafs.Dispose();
        }
    }
}