using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
   
    public class GameFrameXDownloadCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(UnityWebRequestDownloadAgentHelper);
            _ = typeof(DownloadAgentHelperCompleteEventArgs);
            _ = typeof(DownloadAgentHelperErrorEventArgs);
            _ = typeof(DownloadAgentHelperUpdateBytesEventArgs);
            _ = typeof(DownloadAgentHelperUpdateLengthEventArgs);
            _ = typeof(DownloadFailureEventArgs);
            _ = typeof(DownloadManager);
            _ = typeof(DownloadStartEventArgs);
            _ = typeof(DownloadSuccessEventArgs);
            _ = typeof(DownloadUpdateEventArgs);
        }
    }
}