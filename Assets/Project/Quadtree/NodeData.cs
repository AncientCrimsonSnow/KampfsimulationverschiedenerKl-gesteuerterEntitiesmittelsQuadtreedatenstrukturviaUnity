
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Development;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Quadtree
{
    public struct NodeData : ISharedComponentData, IEquatable<NodeData>
    {
        public int index;
        public float4 bounds;
        public float2 center;

        public readonly List<float4> GetEdges()
        {
            return new List<float4>
            {
                bounds.xyxw,
                bounds.zyzw,
                bounds.xwzw,
                bounds.xyzy
            };
        }
        
        public readonly List<float2> GetCorners()
        {
            return new List<float2>
            {
                bounds.xy,
                bounds.xw,
                bounds.zw,
                bounds.zy
            };
        }

        public readonly bool InterceptWithCircle(float2 center, float radius)
        {
            return GetCorners().Any(corner => MyMath.PointIsInCircle(corner, center, radius));
        }

        public bool Equals(NodeData other)
        {
            return index == other.index;
        }

        public override bool Equals(object obj)
        {
            return obj is NodeData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return index;
        }
        
        public static bool operator ==(NodeData a, NodeData b) => a.index == b.index;

        public static bool operator !=(NodeData a, NodeData b) => !(a == b);
        
        public readonly bool IsInBounds(float2 pos)
        {
            return bounds.x <= pos.x &&
                   bounds.y <= pos.y &&
                   bounds.z >= pos.x &&
                   bounds.w >= pos.y;
        }
    }
}