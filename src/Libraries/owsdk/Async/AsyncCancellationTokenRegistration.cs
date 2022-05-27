//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.

using System;

namespace sbio.owsdk.Async
{
  public struct AsyncCancellationTokenRegistration : IEquatable<AsyncCancellationTokenRegistration>,
    IDisposable
  {
    public void Dispose()
    {
      if (m_Source != null)
      {
        m_Source.UnregisterCancellation(this);
      }
    }

    public bool Equals(AsyncCancellationTokenRegistration reg)
    {
      return m_Source == reg.m_Source
             && m_RegID == reg.m_RegID;
    }

    public override bool Equals(object obj)
    {
      if (obj is AsyncCancellationTokenRegistration)
      {
        return Equals((AsyncCancellationTokenRegistration)obj);
      }

      return false;
    }

    public override int GetHashCode()
    {
      return m_RegID;
    }

    public static bool operator ==(AsyncCancellationTokenRegistration lhs, AsyncCancellationTokenRegistration rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(AsyncCancellationTokenRegistration lhs, AsyncCancellationTokenRegistration rhs)
    {
      return !lhs.Equals(rhs);
    }

    internal AsyncCancellationTokenRegistration(AsyncCancellationTokenSource source, int regID, Action callback)
    {
      m_Source = source;
      m_RegID = regID;
      m_Callback = callback;
    }

    internal Action Callback
    {
      get { return m_Callback; }
    }

    private readonly AsyncCancellationTokenSource m_Source;
    private readonly int m_RegID;
    private readonly Action m_Callback;
  }
}


//Copyright SimBlocks LLC 2016-2022
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
