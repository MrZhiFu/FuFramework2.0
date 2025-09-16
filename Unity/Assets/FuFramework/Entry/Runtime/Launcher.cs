using UnityEngine;
using FuFramework.Core.Runtime;

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
            ModuleManager.Instance.RegisterAllModules();
        }
    }
}