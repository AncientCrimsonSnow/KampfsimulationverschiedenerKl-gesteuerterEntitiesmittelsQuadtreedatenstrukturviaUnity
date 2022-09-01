using Unity.Collections;
using Unity.Mathematics;

namespace Project.Quadtree.AStart
{
    public static class Native2dArrayUtils
    {
        public static NativeArray<T> CreateArray <T> (int width, int height, Allocator allocator) where T : struct
        {
            return new NativeArray<T>(width * height, allocator);
        }
        
        public static int CoordinatesToFlatIndex(int2 cellIndex, int width)
        {
            return width * cellIndex.y + cellIndex.x;
        }
        
        public static int2 FlatIndexToCoordinates(int flatIndex, int width)
        {
            var coordinates = new int2
            {
                x = flatIndex % width,
                y = flatIndex / width
            };
            return coordinates;
        }
    }
}