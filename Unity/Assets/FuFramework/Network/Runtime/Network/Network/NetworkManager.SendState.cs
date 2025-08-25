using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    public sealed partial class NetworkManager
    {
        public sealed class SendState : IDisposable
        {
            private const int DefaultBufferLength = 1024 * 64;
            private bool m_Disposed;

            public MemoryStream Stream { get; private set; } = new(DefaultBufferLength);

            public void Reset()
            {
                Stream.Position = 0L;
                Stream.SetLength(0L);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (m_Disposed) return;

                if (disposing && Stream != null)
                {
                    Stream.Dispose();
                    Stream = null;
                }

                m_Disposed = true;
            }
        }
    }
}