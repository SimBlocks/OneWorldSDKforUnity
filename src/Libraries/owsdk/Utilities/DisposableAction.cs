//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Utilities
{
  /// <summary>
  /// IDisposable implementation which invokes a given Action on dispose.
  /// Used for trivial IDisposable situations
  /// </summary>
  public class DisposableAction : IDisposable
  {
    public void Dispose()
    {
      if (m_Disposed)
      {
        throw new ObjectDisposedException(ToString());
      }

      m_Disposed = true;
      m_DisposeAction();
    }

    public DisposableAction(Action disposeAction)
    {
      m_DisposeAction = disposeAction;
    }

#if DEBUG
    ~DisposableAction()
    {
      if (!m_Disposed)
      {
        System.Diagnostics.Trace.TraceWarning("'{0}' was not disposed", this);
      }
    }
#endif

    private readonly Action m_DisposeAction;

    private bool m_Disposed;
  }
}


