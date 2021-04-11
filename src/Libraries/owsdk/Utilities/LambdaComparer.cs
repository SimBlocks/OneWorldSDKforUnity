//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;

namespace sbio.owsdk.Utilities
{
  public static class LambdaComparer
  {
    public static LambdaComparer<T> Create<T>(Func<T, T, bool> func)
    {
      return new LambdaComparer<T>(func);
    }
  }

  public struct LambdaComparer<T> : IEqualityComparer<T>
  {
    public bool Equals(T x, T y)
    {
      return m_Expression(x, y);
    }

    public int GetHashCode(T obj)
    {
      /*
       If you just return 0 for the hash the Equals comparer will kick in. 
       The underlying evaluation checks the hash and then short circuits the evaluation if it is false.
       Otherwise, it checks the Equals. If you force the hash to be true (by assuming 0 for both objects), 
       you will always fall through to the Equals check which is what we are always going for.
      */
      return 0;
    }

    public LambdaComparer(Func<T, T, bool> lambda)
    {
      m_Expression = lambda;
    }

    private readonly Func<T, T, bool> m_Expression;
  }
}


