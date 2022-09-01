using Unity.Collections;

namespace Project.Quadtree.AStart
{
    public static class NativeListExtensions
    {
        public static void InsertOnSortedList(this NativeList<int> list, int value)
        {
            var index = list.BinarySearch(value);
            if (index < 0)
                index = ~index;
            
            list.Insert(index, value);
        }
        
        public static void Insert(this NativeList<int> list, int index, int value)
        {
            if (list.Length + 1 > list.Capacity)
                list.Resize(list.Length + 1, NativeArrayOptions.UninitializedMemory);

            list.Length += 1;

            for (var i = list.Length - 2; i != index - 1; i--)
                list[i + 1] = list[i];
            
            list[index] = value;
        }
    }
}