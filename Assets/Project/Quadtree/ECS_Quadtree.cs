using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Project.Quadtree
{ 
    public class ECS_Quadtree
    {
        private readonly List<ECS_Node> _allNodes = new List<ECS_Node>();

        public List<ECS_Node> AllNodes => _allNodes;

        public readonly ECS_Node root;
        
        private int _nextNodeIndex = -1;

        private ECS_Node GetNode(int index)
        {
            return _allNodes[index];
        }
        public ECS_Node GetNode(NodeData data)
        {
            return GetNode(data.index);
        }

        public void SetNode(ECS_Node node)
        {
            if(_allNodes.Count <= node.data.index)
                _allNodes.Add(node);
            else
                _allNodes[node.data.index] = node; 
        }
        public int GetNextNodeIndex()
        {
            _nextNodeIndex++;
            return _nextNodeIndex;
        }
        
        public ECS_Quadtree(float4 bounds, int startDepth = 0)
        {
            root = new ECS_Node(this, new NodeData
            {
                index = GetNextNodeIndex(),
                bounds = bounds,
                center = ECS_Node.GetCenter(bounds)
            });

            var childs = new HashSet<ECS_Node>{root};
            for (var i = 0; i != startDepth; i++)
            {
                var leafs = new HashSet<ECS_Node>(childs);
                childs = new HashSet<ECS_Node>();
                foreach (var leaf in leafs)
                {
                    leaf.CreateChilds();
                    foreach (var child in leaf.Childs)
                    {
                        childs.Add(child);
                    }
                }
            }
        }
        
        public NodeData FindNodeData(float2 pos)
        {
            var result = root;

            while (result.HasChilds)
                result = result.GetChildNode(pos);

            return result.data;
        }
        
        public HashSet<NodeData> GetAllLeafsData(NodeData startNode)
        {
            var ecsNode = _allNodes[startNode.index];
            var result = new HashSet<NodeData>();
            GetAllLeafsData(ecsNode, ref result);
            return result;
        }
        public HashSet<NodeData> GetAllLeafsData()
        {
            return GetAllLeafsData(root.data);
        }

        public HashSet<ECS_Node> GetAllLeafNodes()
        {
            return GetAllLeafNodes(root);
        }
        
        private HashSet<ECS_Node> GetAllLeafNodes(ECS_Node startNode)
        {
            var result = new HashSet<ECS_Node>();
            
            if (!startNode.HasChilds)
            {
                result.Add(startNode);
            }
            else
            {
                foreach (var child in startNode.Childs)
                {
                    result.UnionWith(GetAllLeafNodes(child));
                }
            }
            
            return result;
        }

        public List<NodeData> GetAllLeafsInCircle(float2 center, float radius)
        {
            var start = FindNode(center);
            var result = new List<NodeData> { start.data };

            var nodesToCheck = new List<ECS_Node>();

            nodesToCheck.AddRange(start.neighbourNodes.botSide);
            nodesToCheck.AddRange(start.neighbourNodes.leftSide);
            nodesToCheck.AddRange(start.neighbourNodes.topSide);
            nodesToCheck.AddRange(start.neighbourNodes.rightSide);

            while (nodesToCheck.Any())
            {
                var nodeToCheck = nodesToCheck.First();
                if (nodeToCheck.data.InterceptWithCircle(center, radius))
                {
                    result.Add(nodeToCheck.data);
                    nodesToCheck.AddRange(nodeToCheck.neighbourNodes.botSide.Where(neighbourNode => !neighbourNode.visited));
                    nodesToCheck.AddRange(nodeToCheck.neighbourNodes.leftSide.Where(neighbourNode => !neighbourNode.visited));
                    nodesToCheck.AddRange(nodeToCheck.neighbourNodes.topSide.Where(neighbourNode => !neighbourNode.visited));
                    nodesToCheck.AddRange(nodeToCheck.neighbourNodes.rightSide.Where(neighbourNode => !neighbourNode.visited));
                }
            }
            
            return result;
        }
        
        private ECS_Node FindNode(float2 pos)
        {
            var result = root;

            while (result.HasChilds)
                result = result.GetChildNode(pos);

            return result;
        }


        public static void ClearSubtree(ECS_Node headNode)
        {
            if (!headNode.HasChilds)
                return;
            
            foreach (var child in headNode.Childs)
                ClearSubtree(child);
            
            headNode.KillChilds();
        }

        private static void GetAllLeafsData(ECS_Node startNode, ref HashSet<NodeData> result)
        {
            if (!startNode.HasChilds)
            {
                result.Add(startNode.data);
            }
            else
            {
                foreach (var child in startNode.Childs)
                {
                    GetAllLeafsData(child, ref result);
                }
            }
        }
    }
}