using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 入口类，用于启动游戏
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        private void Awake()
        {
            GlobalModule.RegisterModule();
        }

        private void Start()
        {
            GlobalModule.InitModule();
        }

        private void Update()
        {
            GlobalModule.UpdateModule(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}