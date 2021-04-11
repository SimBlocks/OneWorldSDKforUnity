//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections.Generic;

namespace sbio.owsdk.Async
{
  //Provides an interface similar to csharp CancellationTokenSource,
  //but for use in older .NET versions than 4.0
  public sealed class AsyncCancellationTokenSource : IDisposable
  {
    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;

      m_CancellationRegistrations.Clear();
    }

    public bool IsCancellationRequested
    {
      get
      {
        if (m_IsCancellationRequested)
        {
          return true;
        }

        if (m_TimeoutStart.HasValue)
        {
          if (DateTime.Now - m_TimeoutStart.Value > m_Timeout.Value)
          {
            Cancel();
          }
        }

        return m_IsCancellationRequested;
      }
    }

    public AsyncCancellationToken Token
    {
      get
      {
        if (m_Disposed)
        {
          throw new ObjectDisposedException(ToString());
        }

        return new AsyncCancellationToken(this);
      }
    }

    public AsyncCancellationTokenSource()
    {
    }

    public AsyncCancellationTokenSource(int timeoutMillis)
      : this(TimeSpan.FromMilliseconds(timeoutMillis))
    {
    }

    public AsyncCancellationTokenSource(TimeSpan timeout)
    {
      CancelAfter(timeout);
    }

    public void Cancel()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      if (!m_IsCancellationRequested)
      {
        m_IsCancellationRequested = true;
        var handlerCount = m_CancellationRegistrations.Count;
        if (handlerCount != 0)
        {
          var exceptions = default(List<Exception>);
          var handlers = new AsyncCancellationTokenRegistration[handlerCount];
          m_CancellationRegistrations.CopyTo(handlers);
          for (var i = 0; i < handlerCount; ++i)
          {
            try
            {
              handlers[i].Callback();
            }
            catch (Exception e)
            {
              if (exceptions == null)
              {
                exceptions = new List<Exception>();
              }

              exceptions.Add(e);
            }
          }

          if (exceptions != null)
          {
            throw new AggregateException(exceptions);
          }
        }
      }
    }

    public void CancelAfter(int millisecondDelay)
    {
      if (millisecondDelay < 0)
      {
        throw new ArgumentOutOfRangeException(nameof(millisecondDelay));
      }

      CancelAfter(TimeSpan.FromMilliseconds(millisecondDelay));
    }

    public void CancelAfter(TimeSpan delay)
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_TimeoutStart = DateTime.Now;
      m_Timeout = delay;
    }

    internal AsyncCancellationTokenRegistration RegisterCancellation(Action callback)
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      if (callback == null)
      {
        throw new ArgumentNullException(nameof(callback), "callback is null");
      }

      var reg = new AsyncCancellationTokenRegistration(this, m_RegCounter++, callback);

      if (IsCancellationRequested)
      {
        callback();
      }
      else
      {
        m_CancellationRegistrations.Add(reg);
      }

      return reg;
    }

    internal void UnregisterCancellation(AsyncCancellationTokenRegistration reg)
    {
      m_CancellationRegistrations.Remove(reg);
    }

    private readonly HashSet<AsyncCancellationTokenRegistration> m_CancellationRegistrations = new HashSet<AsyncCancellationTokenRegistration>();
    private bool m_Disposed;
    private bool m_IsCancellationRequested;
    private DateTime? m_TimeoutStart;
    private TimeSpan? m_Timeout;
    private int m_RegCounter = 0;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
