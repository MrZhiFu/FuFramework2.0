using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Download.Runtime
{
    [Preserve]
    public class GameFrameXDownloadCroppingHelper : MonoBehaviour
    {
        [Preserve]
        private void Start()
        {
            _ = typeof(UnityWebRequestDownloadAgentHelper);
            _ = typeof(Constant);
            _ = typeof(DownloadAgentHelperCompleteEventArgs);
            _ = typeof(DownloadAgentHelperErrorEventArgs);
            _ = typeof(DownloadAgentHelperUpdateBytesEventArgs);
            _ = typeof(DownloadAgentHelperUpdateLengthEventArgs);
            _ = typeof(DownloadFailureEventArgs);
            _ = typeof(DownloadManager);
            _ = typeof(DownloadStartEventArgs);
            _ = typeof(DownloadSuccessEventArgs);
            _ = typeof(DownloadUpdateEventArgs);
            _ = typeof(IDownloadAgentHelper);
            _ = typeof(IDownloadManager);
            _ = typeof(DownloadAgentHelperBase);
            _ = typeof(DownloadComponent);
        }
    }
}