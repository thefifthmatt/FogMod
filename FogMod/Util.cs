using System;
using System.Collections.Generic;

namespace FogMod
{
    public class Util
    {
        public static void AddMulti<K, V>(IDictionary<K, List<V>> dict, K key, V value)
        {
            if (!dict.ContainsKey(key)) dict[key] = new List<V>();
            dict[key].Add(value);
        }
        public static void AddMulti<K, V>(IDictionary<K, List<V>> dict, K key, IEnumerable<V> values)
        {
            if (!dict.ContainsKey(key)) dict[key] = new List<V>();
            dict[key].AddRange(values);
        }
        public static void AddMulti<K, V>(IDictionary<K, HashSet<V>> dict, K key, V value)
        {
            if (!dict.ContainsKey(key)) dict[key] = new HashSet<V>();
            dict[key].Add(value);
        }
        public static void AddMulti<K, V>(IDictionary<K, HashSet<V>> dict, K key, IEnumerable<V> values)
        {
            if (!dict.ContainsKey(key)) dict[key] = new HashSet<V>();
            dict[key].UnionWith(values);
        }
        public static void AddMulti<K, V>(IDictionary<K, SortedSet<V>> dict, K key, V value)
        {
            if (!dict.ContainsKey(key)) dict[key] = new SortedSet<V>();
            dict[key].Add(value);
        }
        public static void Shuffle<T>(Random random, IList<T> list)
        {
            // Fisher Yates shuffle - O(n)
            for (var i = 0; i < list.Count - 1; i++)
            {
                int j = random.Next(i, list.Count);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
        public static void CopyAll<T>(T source, T target)
        {
            var type = typeof(T);
            foreach (System.Reflection.PropertyInfo sourceProperty in type.GetProperties())
            {
                System.Reflection.PropertyInfo targetProperty = type.GetProperty(sourceProperty.Name);
                if (sourceProperty.CanWrite)
                {
                    targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
                }
                else if (sourceProperty.PropertyType.IsArray)
                {
                    Array arr = (Array)sourceProperty.GetValue(source);
                    Array.Copy(arr, (Array)targetProperty.GetValue(target), arr.Length);
                }
                else
                {
                    // Sanity check
                    // Console.WriteLine($"Can't copy {type.Name} {sourceProperty.Name} of type {sourceProperty.PropertyType}");
                }
            }
        }
        public static int SearchBytes(byte[] array, byte[] candidate)
        {
            // byte[] candidate = BitConverter.GetBytes(num);
            for (int i = 0; i < array.Length; i++)
            {
                if (IsMatch(array, i, candidate)) return i;
            }
            return -1;
        }
        private static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }
    }
}
