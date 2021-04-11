//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;
using System.Collections;
using System.Collections.Generic;

namespace sbio.owsdk.Async
{
  public class AsyncTask : IDisposable
    , IEnumerator<bool>
  {
    public static AsyncTask CompletedTask
    {
      get { return sc_CompletedTask.Value; }
    }

    public void Dispose()
    {
      Dispose(true);
    }

    public bool MoveNext()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      if (IsCompleted)
      {
        return false;
      }

      if (m_CancellationToken.IsCancellationRequested && m_CancellablePosition)
      {
        m_Cancelled = true;
        return false;
      }

      try
      {
        if (m_Routine.MoveNext())
        {
          m_CancellablePosition = m_Routine.Current;
          if (m_CancellationToken.IsCancellationRequested && m_CancellablePosition)
          {
            m_Cancelled = true;
          }
        }
        else
        {
          m_CompletedSuccessfully = true;
        }
      }
      catch (AsyncOperationCancelledException e)
      {
        if (e.CancellationToken.Equals(m_CancellationToken))
        {
          m_Cancelled = true;
        }
        else
        {
          m_Exception = e;
        }
      }
      catch (Exception e)
      {
        m_Exception = e;
      }

      return !IsCompleted;
    }

    public bool Current
    {
      get { return m_CancellablePosition; }
    }

    object IEnumerator.Current
    {
      get { return m_CancellablePosition; }
    }

    public void Reset()
    {
      throw new NotImplementedException();
    }

    public void Wait()
    {
      if (!IsCompleted)
      {
        if (m_CancellationToken.IsCancellationRequested && m_CancellablePosition)
        {
          m_Cancelled = true;
        }
        else
        {
          try
          {
            while (m_Routine.MoveNext())
            {
              if (m_CancellationToken.IsCancellationRequested && m_CancellationToken.IsCancellationRequested.Equals(m_Routine.Current))
              {
                m_Cancelled = true;
                break;
              }
            }

            if (!m_Cancelled)
            {
              m_CompletedSuccessfully = true;
            }
          }
          catch (AsyncOperationCancelledException e)
          {
            if (m_CancellationToken.Equals(e.CancellationToken))
            {
              m_Cancelled = true;
            }
            else
            {
              m_Exception = e;
            }
          }
          catch (Exception e)
          {
            m_Exception = e;
          }
        }
      }

      if (IsFaulted)
      {
        throw Exception;
      }

      if (IsCancelled)
      {
        //Only throw if we received said cancellation in time
        m_CancellationToken.ThrowIfCancellationRequested();
      }
    }

    public Exception Exception
    {
      get { return m_Exception; }
    }

    public bool IsCancelled
    {
      get { return m_Cancelled; }
    }

    public bool IsCompleted
    {
      get { return m_CompletedSuccessfully || IsCancelled || IsFaulted; }
    }

    public bool IsFaulted
    {
      get { return Exception != null; }
    }

    public AsyncTask(IEnumerator<bool> routine)
      : this(routine, AsyncCancellationToken.None)
    {
    }

    public AsyncTask(IEnumerator<bool> routine, AsyncCancellationToken tok)
    {
      if (routine == null)
      {
        throw new ArgumentNullException(nameof(routine));
      }

      m_Routine = routine;
      m_CancellationToken = tok;
      //Allow cancellation before we even begin
      m_CancellablePosition = true;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;

      //Sever any references
      m_Exception = null;
      m_Routine = null;
    }

    private static AsyncTask CreateCompleted()
    {
      var ret = new AsyncTask(((IEnumerable<bool>)(new bool[0])).GetEnumerator());
      ret.MoveNext();
      return ret;
    }

    private static readonly Lazy<AsyncTask> sc_CompletedTask = new Lazy<AsyncTask>(CreateCompleted);

    private IEnumerator<bool> m_Routine;
    private readonly AsyncCancellationToken m_CancellationToken;
    private bool m_Disposed = false;
    private bool m_CompletedSuccessfully = false;
    private bool m_CancellablePosition;
    private Exception m_Exception;
    private bool m_Cancelled = false;
  }

  public sealed class AsyncTask<T> : AsyncTask
  {
    public T Result
    {
      get
      {
        if (m_Disposed)
        {
          throw new ObjectDisposedException(ToString());
        }

        if (!m_ResultCached)
        {
          Wait();
          m_CachedResult = m_ResultFn();
          m_ResultCached = true;
        }

        return m_CachedResult;
      }
    }

    public AsyncTask(IEnumerator<bool> routine, Func<T> resultFn)
      : this(routine, resultFn, new AsyncCancellationToken(false))
    {
    }

    public AsyncTask(IEnumerator<bool> routine, Func<T> resultFn, AsyncCancellationToken tok)
      : base(routine, tok)
    {
      m_ResultFn = resultFn;
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      if (!m_Disposed)
      {
        if (disposing)
        {
          m_CachedResult = default(T);
        }
      }

      m_Disposed = true;
    }

    private readonly Func<T> m_ResultFn;
    private bool m_Disposed;
    private bool m_ResultCached = false;
    private T m_CachedResult;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
