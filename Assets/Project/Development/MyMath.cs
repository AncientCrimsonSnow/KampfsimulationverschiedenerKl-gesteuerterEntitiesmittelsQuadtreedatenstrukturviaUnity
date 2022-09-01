using Unity.Mathematics;
using UnityEngine;

namespace Project.Development
{
    public static class MyMath
    {
        public static bool PointIsInCircle(float2 point, float2 center, float radius)
        {
            var distance = GetDistance(point, center);
            return radius >= distance;
        }

        
        static float GetDistance(float2 p1, float2 p2)
        {
            return Mathf.Sqrt(Mathf.Pow((p2.x - p1.x), 2) + Mathf.Pow((p2.y - p1.y), 2));
        }
    }
}