//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Extensions
{
  public static class ArrayExtensions
  {
    //Handles special single value null case (at least, it causes an ambiguous reference)
    public static void Fill<T>(this T[] destinationArray, T value)
    {
      if (destinationArray == null)
      {
        throw new ArgumentNullException(nameof(destinationArray));
      }

      if (destinationArray.Length == 0)
      {
        throw new ArgumentException("Length of value array must not be more than length of destination");
      }

      // set the initial array value
      destinationArray[0] = value;

      int copyLength, nextCopyLength;

      for (copyLength = 1; (nextCopyLength = copyLength << 1) < destinationArray.Length; copyLength = nextCopyLength)
      {
        Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);
      }

      Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
    }

    //See https://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value/22867582
    public static void Fill<T>(this T[] destinationArray, params T[] value)
    {
      if (destinationArray == null)
      {
        throw new ArgumentNullException(nameof(destinationArray));
      }

      if (value.Length > destinationArray.Length)
      {
        throw new ArgumentException("Length of value array must not be more than length of destination");
      }

      // set the initial array value
      Array.Copy(value, destinationArray, value.Length);

      int copyLength, nextCopyLength;

      for (copyLength = value.Length; (nextCopyLength = copyLength << 1) < destinationArray.Length; copyLength = nextCopyLength)
      {
        Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);
      }

      Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
    }

    //Handles special single value null case (at least, it causes an ambiguous reference)
    public static void Fill<T>(this ArraySegment<T> destinationArray, T value)
    {
      if (destinationArray.Count == 0)
      {
        throw new ArgumentException("Length of value array must not be more than length of destination");
      }

      // set the initial array value
      destinationArray.Array[0] = value;

      int copyLength, nextCopyLength;

      for (copyLength = 1; (nextCopyLength = copyLength << 1) < destinationArray.Count; copyLength = nextCopyLength)
      {
        Array.Copy(destinationArray.Array, destinationArray.Offset, destinationArray.Array, copyLength + destinationArray.Offset, copyLength);
      }

      Array.Copy(destinationArray.Array, destinationArray.Offset, destinationArray.Array, copyLength + destinationArray.Offset, destinationArray.Count - copyLength);
    }

    //See https://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value/22867582
    public static void Fill<T>(this ArraySegment<T> destinationArray, params T[] value)
    {
      if (value.Length > destinationArray.Count)
      {
        throw new ArgumentException("Length of value array must not be more than length of destination");
      }

      // set the initial array value
      Array.Copy(value, 0, destinationArray.Array, destinationArray.Offset, value.Length);

      int copyLength, nextCopyLength;

      for (copyLength = value.Length; (nextCopyLength = copyLength << 1) < destinationArray.Count; copyLength = nextCopyLength)
      {
        Array.Copy(destinationArray.Array, destinationArray.Offset, destinationArray.Array, copyLength + destinationArray.Offset, copyLength);
      }

      Array.Copy(destinationArray.Array, destinationArray.Offset, destinationArray.Array, copyLength, destinationArray.Count - copyLength);
    }

    /// <summary>
    /// Copy the first `count` elements from `source` into a fresh array
    /// </summary>
    /// <typeparam name="T">The array element type</typeparam>
    /// <param name="source">The source array</param>
    /// <param name="count">The number of elements to copy</param>
    /// <returns>The new array</returns>
    public static T[] CopySegment<T>(this T[] source, int count)
    {
      return CopySegment(source, 0, count);
    }

    /// <summary>
    /// Skip `offset` elements from `source` and copy the following `count` elementsCopy `count` elements from `source`, offset by `offset` into a fresh array
    /// </summary>
    /// <typeparam name="T">The array element type</typeparam>
    /// <param name="source">The source array</param>
    /// <param name="offset">The number of elements to skip</param>
    /// <param name="count">The number of elements to copy</param>
    /// <returns>The new array</returns>
    public static T[] CopySegment<T>(this T[] source, int offset, int count)
    {
      //Arraysegment will do all the arg validation
      return CopySegment(new ArraySegment<T>(source, offset, count));
    }

    /// <summary>
    /// Copies the array segment pointed to by `source` into a fresh array
    /// </summary>
    /// <typeparam name="T">The array element type</typeparam>
    /// <param name="source">The source segment</param>
    /// <returns>A new array containing the elements in `source`</returns>
    public static T[] CopySegment<T>(this ArraySegment<T> source)
    {
      var ary = source.Array;
      var offset = source.Offset;
      var count = source.Count;

      var ret = new T[count];
      Array.Copy(ary, offset, ret, 0, count);
      return ret;
    }
  }
}


