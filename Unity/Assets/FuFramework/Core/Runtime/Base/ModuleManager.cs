using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 框架模块管理器。
    /// 管理(注册和获取)框架中的所有组件
    /// </summary>
    public static class ModuleManager
    {
        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        public const int GameFrameworkSceneId = 0;

        /// <summary>
        /// 记录所有模块组件的链表集合
        /// </summary>
        private static readonly FuLinkedList<FuComponent> AllComponentList = new();

        /// <summary>
        /// 获取游戏框架模块
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架组件类型。</typeparam>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T GetModule<T>() where T : FuComponent => GetModule(typeof(T)) as T;

        /// <summary>
        /// 获取游戏框架组件(通过组件类型)
        /// </summary>
        /// <param name="type">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static FuComponent GetModule(Type type)
        {
            var current = AllComponentList.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                    return current.Value;

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 注册游戏框架模块
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T RegisterModule<T>() where T : FuComponent
        {
            if (GetModule(typeof(T)) != null)
            {
                Log.Error($"要注册的游戏框架组件 '{typeof(T).FullName}' 已存在，不可重复注册!");
                return null;
            }

            var module = UnityEngine.Object.FindObjectOfType<T>();
            if (module == null)
            {
                // 创建模块的GameObject
                var moduleObject = new GameObject();
                module = moduleObject.AddComponent<T>();
                moduleObject.name = $"[Module]_{typeof(T).Name}";
            }

            // 优先级大的组件注册在链表的前面
            var current = AllComponentList.First;
            while (current != null)
            {
                if (module.Priority > current.Value.Priority) break;
                current = current.Next;
            }

            if (current != null)
                AllComponentList.AddBefore(current, module);
            else
                AllComponentList.AddLast(module);
            
            Log.Info($"注册模块 '{typeof(T).Name}' 成功!");
            return module;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            foreach (var module in AllComponentList)
            {
                Log.Info($"初始化模块 '{module.gameObject.name}' 成功!");
                module.OnInit();
            }
        }

        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var module in AllComponentList)
            {
                module.OnUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭游戏框架，退出游戏时调用。由外部代码调用，如设置界面的重启/退出按钮。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型。</param>
        public static void Shutdown(ShutdownType shutdownType)
        {
            Log.Info("关闭游戏框架 ({0})...", shutdownType);

            foreach (var component in AllComponentList)
            {
                component.OnShutdown(shutdownType);
            }

            AllComponentList.Clear();

            switch (shutdownType)
            {
                case ShutdownType.Restart:
                    SceneManager.LoadScene(GameFrameworkSceneId);
                    return;
                case ShutdownType.Quit:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    return;
                default: return;
            }
        }
    }
}