//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
using System;

namespace sbio.owsdk.Async
{
  public sealed class AsyncOperationCancelledException : Exception
  {
    public AsyncCancellationToken CancellationToken
    {
      get { return m_Token; }
    }

    public AsyncOperationCancelledException()
      : this("The operation was cancelled", null, AsyncCancellationToken.None)
    {
    }

    public AsyncOperationCancelledException(string message)
      : this(message, null, AsyncCancellationToken.None)
    {
    }

    public AsyncOperationCancelledException(AsyncCancellationToken token)
      : this("The operation was cancelled", null, token)
    {
    }

    public AsyncOperationCancelledException(string message, Exception innerException)
      : this(message, innerException, AsyncCancellationToken.None)
    {
    }

    public AsyncOperationCancelledException(string message, AsyncCancellationToken token)
      : this(message, null, token)
    {
    }

    public AsyncOperationCancelledException(string message, Exception innerException, AsyncCancellationToken token)
      : base(message, innerException)
    {
      m_Token = token;
    }

    private AsyncCancellationToken m_Token;
  }
}


//Copyright SimBlocks LLC 2021
//https://www.simblocks.io/
//The source code in this file is licensed under the MIT License. See the LICENSE text file for full terms.
