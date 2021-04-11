//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Async
{
  //Provides an interface similar to csharp CancellationToken
  //but for use in older .NET versions than 4.0
  public struct AsyncCancellationToken
  {
    public static AsyncCancellationToken None
    {
      get { return new AsyncCancellationToken(); }
    }

    public bool CanBeCancelled
    {
      get { return m_Source != null; }
    }

    public bool IsCancellationRequested
    {
      get
      {
        if (m_Source != null)
        {
          return m_Source.IsCancellationRequested;
        }


        return m_Cancelled;
      }
    }

    public override int GetHashCode()
    {
      return m_Source == null ? base.GetHashCode() : m_Source.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj is AsyncCancellationToken)
      {
        return Equals((AsyncCancellationToken)obj);
      }

      return false;
    }

    public bool Equals(AsyncCancellationToken other)
    {
      if (m_Source == other.m_Source)
      {
        if (m_Source == null)
        {
          return m_Cancelled == other.m_Cancelled;
        }

        return true;
      }

      return false;
    }

    public void ThrowIfCancellationRequested()
    {
      if (IsCancellationRequested)
      {
        throw new AsyncOperationCancelledException(this);
      }
    }

    public AsyncCancellationTokenRegistration Register(Action callback)
    {
      if (m_Source != null)
      {
        return m_Source.RegisterCancellation(callback);
      }

      return new AsyncCancellationTokenRegistration();
    }

    public AsyncCancellationToken(bool cancelled)
    {
      m_Source = null;
      m_Cancelled = cancelled;
    }

    internal AsyncCancellationTokenSource Source
    {
      get { return m_Source; }
    }

    internal AsyncCancellationToken(AsyncCancellationTokenSource source)
    {
      m_Source = source;
      m_Cancelled = false;
    }

    private readonly AsyncCancellationTokenSource m_Source;
    private readonly bool m_Cancelled;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
