using System.Collections.Generic;
using System.Linq;
using Project.Development.DrawStateMachine.States;
using Project.Development.DrawStateMachine.States.DrawPathStateMachine;
using Project.Quadtree;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Project.Development
{
    public class DebugDrawer : MonoBehaviour
    {
        private ECS_Quadtree _quadtree;

        public HashSet<float4> edgesToDraw = new HashSet<float4>();
        
        private Console _console;
        
        public readonly Color gridColor = Color.green;
        public readonly Color neighbourColor = Color.red;
        public readonly Color startColor = Color.blue;
        public readonly Color endColor = Color.yellow;
        public readonly Color pathColor = Color.magenta;
        public readonly Color textColor = Color.cyan;
        
        private DrawBaseState _crrState;
        
        private DrawNothingState _nothingState;
        private DrawGridState _gridState;
        private DrawNeighbourState _neighbourState;
        private DrawPathState _pathState;
        
        public Console Console => _console;

        public ECS_Quadtree Tree => _quadtree;

        private void Start()
        {
            _console = Console.instance;
            
            _nothingState = new DrawNothingState(this);
            _gridState = new DrawGridState(this);
            _neighbourState = new DrawNeighbourState(this);
            _pathState = new DrawPathState(this);

            DelayedStart();
        }

        private async void DelayedStart()
        {
            await Task.Delay(100);
            _quadtree = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<QuadtreeCoreSystem>().Tree;
            SwitchState();
            UpdateGridLoop();
        }

        private async void UpdateGridLoop()
        {
            while (true)
            {
                await Task.Delay(300);
                UpdateAllEdges();
            }
            
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
            _crrState?.UpdateGizmo();
        }

        private void Update()
        {
            _crrState?.Update();
        }
        
        public void SwitchState()
        {
            switch (_console.DrawingMode)
            {
                case DrawingMode.NONE:
                    _crrState = _nothingState;
                    break;
                case DrawingMode.SIMPLE_GRID:
                    _crrState = _gridState;
                    _crrState.Enter();
                    break;
                case DrawingMode.NEIGHBOUR_GRID:
                    _crrState = _neighbourState;
                    _crrState.Enter();
                    break;
                case DrawingMode.WEIGHTS:
                case DrawingMode.SIMPLE_PATH:
                case DrawingMode.FLOW_FIELD:
                case DrawingMode.BEZIER_PATH:
                    _crrState = _pathState;
                    _crrState.Enter();
                    break;
            }
        }

        public void UpdateAllEdges()
        {
            edgesToDraw.Clear();
            var leafs = _quadtree.GetAllLeafsData();
            foreach (var edge in leafs.Select(leaf => leaf.GetEdges()).SelectMany(edges => edges))
            {
                edgesToDraw.Add(edge);
            }
        }
    }
}