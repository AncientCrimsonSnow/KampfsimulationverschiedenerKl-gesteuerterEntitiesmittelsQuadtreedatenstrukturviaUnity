using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Quadtree
{
    public class ECS_Node
    {
        public bool HasChilds { get; private set; }
        public ECS_Node[] Childs { get; private set; }

        public NodeNeighbours neighbourNodes;
        public bool visited;
        
        public readonly NodeData data;
        public readonly ECS_Node parent;

        private readonly ECS_Quadtree _tree;
        private readonly float4[] _childBounds;
        
        
        public int cost;

        private int[] _childIndices; 

        public ECS_Node(ECS_Quadtree tree, NodeData data)
        {
            _tree = tree;
            this.data = data;
            parent = null;
            _childBounds = new[]
            {
                CalcBotLeftChildBounds(),
                CalcTopLeftChildBounds(),
                CalcTopRightBounds(),
                CalcBotRightBounds()
            };
            _tree.SetNode(this);
        }

        public ECS_Node(ECS_Quadtree tree, NodeData data, ECS_Node parent)
        { 
            _tree = tree;
            this.data = data;
            this.parent = parent;
            _childBounds = new[]
            {
                CalcBotLeftChildBounds(),
                CalcTopLeftChildBounds(),
                CalcTopRightBounds(),
                CalcBotRightBounds()
            };
            
            _tree.SetNode(this);
        }
        
        public void CreateChilds()
        {
            Childs = new ECS_Node[4];
            if (_childIndices is null)
            {
                _childIndices = new int[4];
                for (byte i = 0; i != _childIndices.Length; i++)
                {
                    _childIndices[i] = _tree.GetNextNodeIndex();
                    Childs[i] = new ECS_Node(_tree, new NodeData
                    {
                        index = _childIndices[i],
                        bounds = _childBounds[i],
                        center = GetCenter(_childBounds[i])
                    }, this);
                }
            }
            else
            {
                for (byte i = 0; i != _childIndices.Length; i++)
                {
                    Childs[i] = new ECS_Node(_tree, new NodeData
                    {
                        index = _childIndices[i],
                        bounds = _childBounds[i],
                        center = GetCenter(_childBounds[i])
                    }, this);
                }
            }
            SetChildsNeighbours();
            HasChilds = true;
        }

        private void SetChildsNeighbours()
        {
            Childs[0].neighbourNodes = neighbourNodes.GetNeighboursOfBotLeftChild(Childs[0].data.bounds, Childs);
            Childs[1].neighbourNodes = neighbourNodes.GetNeighboursOfTopLeftChild(Childs[1].data.bounds, Childs);
            Childs[2].neighbourNodes = neighbourNodes.GetNeighboursOfTopRightChild(Childs[2].data.bounds, Childs);
            Childs[3].neighbourNodes = neighbourNodes.GetNeighboursOfBotRightChild(Childs[3].data.bounds, Childs);
            UpdateSideNodesAtSplit();
        }
        
        public void KillChilds()
        {
            MergeChildNeighbours();
            UpdateSideNodesAtMerge();
            Childs = null;
            HasChilds = false;
        }

        private void UpdateSideNodesAtMerge()
        {
            var newSide = new HashSet<ECS_Node> { this };
            if (!(neighbourNodes.leftSide is null))
            {
                foreach (var sideNode in neighbourNodes.leftSide)
                    sideNode.UpdateRightSide(newSide, new HashSet<ECS_Node> { Childs[0], Childs[1] });
            }
            if (!(neighbourNodes.topSide is null))
            {
                foreach (var sideNode in neighbourNodes.topSide)
                    sideNode.UpdateBotSide(newSide, new HashSet<ECS_Node> { Childs[1], Childs[2] });
            }
            if (!(neighbourNodes.rightSide is null))
            {
                foreach (var sideNode in neighbourNodes.rightSide)
                    sideNode.UpdateLeftSide(newSide, new HashSet<ECS_Node> { Childs[2], Childs[3] });
            }
            if (!(neighbourNodes.botSide is null))
            {
                foreach (var sideNode in neighbourNodes.botSide)
                    sideNode.UpdateTopSide(newSide, new HashSet<ECS_Node>{ Childs[0], Childs[3] });
            }
        }
        
        private void UpdateSideNodesAtSplit()
        {
            if (!(neighbourNodes.leftSide is null))
            {
                var newLeftSide = new HashSet<ECS_Node> { Childs[0], Childs[1] };
                foreach (var sideNode in neighbourNodes.leftSide)
                    sideNode.UpdateRightSide(newLeftSide, new HashSet<ECS_Node>{this});
            }
            if (!(neighbourNodes.topSide is null))
            {
                var newTopSide = new HashSet<ECS_Node> { Childs[1], Childs[2] };
                foreach (var sideNode in neighbourNodes.topSide)
                    sideNode.UpdateBotSide(newTopSide, new HashSet<ECS_Node>{this});
            }
            if (!(neighbourNodes.rightSide is null))
            {
                var newRightSide = new HashSet<ECS_Node> { Childs[2], Childs[3] };
                foreach (var sideNode in neighbourNodes.rightSide)
                    sideNode.UpdateLeftSide(newRightSide, new HashSet<ECS_Node>{this});
            }
            if (!(neighbourNodes.botSide is null))
            {
                var newBotSide = new HashSet<ECS_Node> { Childs[3], Childs[0] };
                foreach (var sideNode in neighbourNodes.botSide)
                    sideNode.UpdateTopSide(newBotSide, new HashSet<ECS_Node>{this});
            }
        }

        public ECS_Node GetChildNode(float2 pos)
        {
            return Childs[GetSplitBoundsIndex(pos)];
        }
        
        public static float2 GetCenter(float4 bounds)
        {
            return new float2
            {
                x = (bounds.x + bounds.z) / 2,
                y = (bounds.y + bounds.w) / 2
            };
        }

        private void UpdateLeftSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes)
        {
            neighbourNodes.UpdateLeftSide(newSideNodes, oldSideNodes, data.bounds);
        }

        private void UpdateTopSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes)
        {
            neighbourNodes.UpdateTopSide(newSideNodes, oldSideNodes, data.bounds);
        }

        private void UpdateRightSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes)
        {
            neighbourNodes.UpdateRightSide(newSideNodes, oldSideNodes, data.bounds);
        }

        private void UpdateBotSide(HashSet<ECS_Node> newSideNodes, HashSet<ECS_Node> oldSideNodes)
        {
            neighbourNodes.UpdateBotSide(newSideNodes, oldSideNodes, data.bounds);
        }

        private void MergeChildNeighbours()
        {
            if (!(neighbourNodes.leftSide is null))
                neighbourNodes.leftSide = Combine(Childs[0].neighbourNodes.leftSide, Childs[1].neighbourNodes.leftSide);
            if(!(neighbourNodes.topSide is null))
                neighbourNodes.topSide = Combine(Childs[1].neighbourNodes.topSide, Childs[2].neighbourNodes.topSide);
            if(!(neighbourNodes.rightSide is null))
                neighbourNodes.rightSide = Combine(Childs[2].neighbourNodes.rightSide, Childs[3].neighbourNodes.rightSide);
            if(!(neighbourNodes.botSide is null))
                neighbourNodes.botSide = Combine(Childs[3].neighbourNodes.botSide, Childs[0].neighbourNodes.botSide);
        }

        private HashSet<ECS_Node> Combine(HashSet<ECS_Node> list1, HashSet<ECS_Node> list2)
        {
            var result = new HashSet<ECS_Node>(list1);
            result.UnionWith(list2);
            return result;
        } 


        private float4 CalcBotLeftChildBounds()
        {
            return new float4
            {
                x = data.bounds.x,
                y = data.bounds.y,
                z = data.center.x,
                w = data.center.y
            };
        }

        private float4 CalcTopLeftChildBounds()
        {
            return new float4
            {
                x = data.bounds.x,
                y = data.center.y,
                z = data.center.x,
                w = data.bounds.w
            };
        }

        private float4 CalcTopRightBounds()
        {
            return new float4
            {
                x = data.center.x,
                y = data.center.y,
                z = data.bounds.z,
                w = data.bounds.w
            };
        }
        private float4 CalcBotRightBounds()
        {
            return new float4
            {
                x = data.center.x,
                y = data.bounds.y,
                z = data.bounds.z,
                w = data.center.y
            };
        }
        
        private int GetSplitBoundsIndex(float2 pos)
        {
            return GetSplitBoundsIndex(pos, data.center);
        }
        

        public static int GetSplitBoundsIndex(float2 pos, float2 center)
        {
            if (pos.x <= center.x)
                return pos.y <= center.y ? 0 : 1;

            return pos.y <= center.y ? 3 : 2;
        }
    }
}