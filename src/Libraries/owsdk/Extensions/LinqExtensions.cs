//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;
using System.Linq;
using sbio.owsdk.Utilities;

namespace sbio.owsdk.Extensions
{
  public static class LinqExtensions
  {
    public static bool Contains<T>(this IEnumerable<T> first, T value, Func<T, T, bool> comparer)
    {
      return first.Contains(value, new LambdaComparer<T>(comparer));
    }

    public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
    {
      return source.Distinct(new LambdaComparer<T>(comparer));
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
    {
      return first.Except(second, new LambdaComparer<T>(comparer));
    }

    public static IEnumerable<T> Intersect<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
    {
      return first.Intersect(second, new LambdaComparer<T>(comparer));
    }
  }
}



