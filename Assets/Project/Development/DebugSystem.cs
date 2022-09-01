using Project.Quadtree;
using Unity.Entities;
using UnityEngine;

namespace Project.Development
{
    public partial class DebugSystem : SystemBase
    {
        private ECS_Quadtree _tree;

        private const float radius = 5;

        protected override void OnStartRunning()
        {
            _tree = World.GetOrCreateSystem<QuadtreeCoreSystem>().Tree;
        }


        protected override void OnUpdate()
        {
            /*
            if (Input.GetMouseButtonDown(0))
            {
                var entitiesInCircle = _tree.GetAllLeafs();
            }
            */
        }
    }
}