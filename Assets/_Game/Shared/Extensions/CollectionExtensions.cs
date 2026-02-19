using System;
using System.Collections.Generic;

namespace Game.Shared.Extensions
{
    public static class CollectionExtensions
    {
        public static T RandomElement<T>(this IReadOnlyList<T> list, Random rng)
        {
            if (list.Count == 0) throw new InvalidOperationException("Collection is empty.");
            return list[rng.Next(list.Count)];
        }

        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
