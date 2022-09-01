using System.Collections.Generic;
using Unity.Mathematics;

namespace Project.Quadtree
{
    public struct NodeNeighbours
    {
        public HashSet<ECS_Node> leftSide;
        public HashSet<ECS_Node> topSide;
        public HashSet<ECS_Node> rightSide;
        public HashSet<ECS_Node> botSide;
        
        public NodeNeighbours GetNeighboursOfBotLeftChild(float4 childBounds, ECS_Node[] childs)
        {
            return new NodeNeighbours
            {
                leftSide = CheckVerticalNeighbourNodes(leftSide,  childBounds),
                topSide = new HashSet<ECS_Node> { childs[1] },
                rightSide = new HashSet<ECS_Node> { childs[3] },
                botSide = CheckHorizontalNeighbourNodes(botSide,  childBounds)
            };
        }
        public NodeNeighbours GetNeighboursOfTopLeftChild(float4 childBounds, ECS_Node[] childs)
        {
            return new NodeNeighbours
            {
                leftSide = CheckVerticalNeighbourNodes(leftSide,  childBounds),
                topSide = CheckHorizontalNeighbourNodes(topSide,  childBounds),
                rightSide = new HashSet<ECS_Node> { childs[2] },
                botSide = new HashSet<ECS_Node> { childs[0] }
            };
        }
        
        public NodeNeighbours GetNeighboursOfTopRightChild(float4 childBounds, ECS_Node[] childs)
        {
            return new NodeNeighbours
            {
                leftSide = new HashSet<ECS_Node> { childs[1] },
                topSide = CheckHorizontalNeighbourNodes(topSide,  childBounds),
                rightSide = CheckVerticalNeighbourNodes(rightSide,  childBounds),
                botSide = new HashSet<ECS_Node> { childs[3] }
            };
        }
        
        public NodeNeighbours GetNeighboursOfBotRightChild(float4 childBounds, ECS_Node[] childs)
        {
            var result = new NodeNeighbours
            {
                leftSide = new HashSet<ECS_Node> { childs[0] },
                topSide = new HashSet<ECS_Node> { childs[2] },
                rightSide = CheckVerticalNeighbourNodes(rightSide, childBounds),
                botSide = CheckHorizontalNeighbourNodes(botSide, childBounds),
            };
            return result;
        }

        public void UpdateLeftSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes, float4 bounds)
        {
            if (leftSide is null)
                leftSide = CheckVerticalNeighbourNodes(newSideNodes, bounds);
            else
            {
                leftSide.ExceptWith(oldSideNodes);
                leftSide.UnionWith(CheckVerticalNeighbourNodes(newSideNodes, bounds));
            }
        }

        public void UpdateTopSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes, float4 bounds)
        {
            if (topSide is null)
                topSide = CheckHorizontalNeighbourNodes(newSideNodes, bounds);
            else
            {
                topSide.ExceptWith(oldSideNodes);
                topSide.UnionWith(CheckHorizontalNeighbourNodes(newSideNodes, bounds));
            }
        }

        public void UpdateRightSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes, float4 bounds)
        {
            if (rightSide is null)
                rightSide = CheckVerticalNeighbourNodes(newSideNodes, bounds);
            else
            {
                rightSide.ExceptWith(oldSideNodes);
                rightSide.UnionWith(CheckVerticalNeighbourNodes(newSideNodes, bounds));
            }
        }

        public void UpdateBotSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes, float4 bounds)
        {
            if (botSide is null)
                botSide = CheckHorizontalNeighbourNodes(newSideNodes, bounds);
            else
            {
                botSide.ExceptWith(oldSideNodes);
                botSide.UnionWith(CheckHorizontalNeighbourNodes(newSideNodes, bounds));
            }
        }
        
        
        private HashSet<ECS_Node> CheckVerticalNeighbourNodes(HashSet<ECS_Node> sideNeighbourNodes, float4 bounds)
        {
            if (sideNeighbourNodes is null)
                return null;
            
            var result = new HashSet<ECS_Node>();
            foreach (var sideNeighbourNode in sideNeighbourNodes)
            {
                if(BoundsOverlapVertical(bounds, sideNeighbourNode.data.bounds))
                    result.Add(sideNeighbourNode);
            }
            return result;
        }
        
        private HashSet<ECS_Node> CheckHorizontalNeighbourNodes(HashSet<ECS_Node> sideNeighbourNodes, float4 bounds)
        {
            if (sideNeighbourNodes is null)
                return null;
            
            var result = new HashSet<ECS_Node>();
            foreach (var sideNeighbourNode in sideNeighbourNodes)
            {
                if(BoundsOverlapHorizontal(bounds, sideNeighbourNode.data.bounds))
                    result.Add(sideNeighbourNode);
            }
            return result;
        }

        private bool LinesInTouch(float line1Min, float line1Max, float line2Min, float line2Max)
        {
            return !(line2Max <= line1Min) && !(line1Max <= line2Min);
        }

        private bool BoundsOverlapVertical(float4 bounds1, float4 bounds2)
        {
            return LinesInTouch(bounds1.y, bounds1.w, bounds2.y, bounds2.w);
        }
        
        private bool BoundsOverlapHorizontal(float4 bounds1, float4 bounds2)
        {
            return LinesInTouch(bounds1.x, bounds1.z, bounds2.x, bounds2.z);
        }
        
    }
}