using UnityEngine;
using FuFramework.Entry.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Download.Runtime;

/// <summary>
/// 下载模块测试用例
/// </summary>
public class DownloadModuleTest : MonoBehaviour
{
    private void Start()
    {
        var eventModule = GlobalModule.EventModule;
        if (eventModule== null) return;
        
        eventModule.Subscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
        eventModule.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
        eventModule.Subscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
        eventModule.Subscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
    }

    private void OnDestroy()
    {
        var eventModule = GlobalModule.EventModule;
        if (eventModule== null) return;
        
        eventModule.Unsubscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
        eventModule.Unsubscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
        eventModule.Unsubscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
        eventModule.Unsubscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DownloadTest("https://img.icons8.com/?size=100&id=108638&format=png&color=000000", "Test.png");
            // DownloadTest("https://goodies.icons8.com/web/landings/home/landing-main_icons.mp4", "TestVedio.mp4");
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