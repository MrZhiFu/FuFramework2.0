using UnityEngine;
using FuFramework.Entry.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Download.Runtime;

/// <summary>
/// 下载模块测试用例
/// </summary>
public class DownloadModuleTest : MonoBehaviour
{
    private void OnEnable()
    {
        GlobalModule.EventModule.Subscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
        GlobalModule.EventModule.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
        GlobalModule.EventModule.Subscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
        GlobalModule.EventModule.Subscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
    }

    private void OnDisable()
    {
        GlobalModule.EventModule.Unsubscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
        GlobalModule.EventModule.Unsubscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
        GlobalModule.EventModule.Unsubscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
        GlobalModule.EventModule.Unsubscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DownloadTest("https://ns-strategy.cdn.bcebos.com/ns-strategy/upload/fc_big_pic/part-00573-3457.jpg", "Test.jpg");
            // DownloadTest("http://xxxx.TestAudio.mp3", "TestAudio.mp3");
            // DownloadTest("http://xxxx.TestDat.dat", "TestDat.dat");
        }
    }

    protected void DownloadTest(string url, string fileName)
    {
        GlobalModule.DownloadModule.AddDownload(Application.persistentDataPath + "/" + fileName, url);
    }

    private void OnDownloadStart(object sender, GameEventArgs e)
    {
        Debug.Log("下载开始");
    }

    private void OnDownloadSuccess(object sender, GameEventArgs e)
    {
        Debug.Log("下载成功");
    }

    private void OnDownloadFailure(object sender, GameEventArgs e)
    {
        Debug.Log("下载失败");
    }

    private void OnDownloadUpdate(object sender, GameEventArgs e)
    {
        Debug.Log("下载更新进度");
    }
}