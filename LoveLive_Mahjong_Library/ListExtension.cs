using System;
using System.Collections.Generic;
using System.Text;

namespace LoveLive_Mahjong_Library
{
    static class ListExtension
    {
        private static Random random = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            for (n = list.Count - 1; n > 0; n--)
            {
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
