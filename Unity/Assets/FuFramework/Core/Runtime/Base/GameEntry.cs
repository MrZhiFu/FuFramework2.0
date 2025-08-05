using System;
using FuFramework.Core.Runtime;
using GameFrameX.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架入口--管理(注册和获取)框架中的所有组件
    /// </summary>
    public static class GameEntry
    {
        /// <summary>
        /// 记录所有模块组件的链表集合
        /// </summary>
        private static readonly GameFrameworkLinkedList<GameFrameworkComponent> s_AllComponentList = new();

        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        public const int GameFrameworkSceneId = 0;

        /// <summary>
        /// 获取游戏框架组件(通过组件类型-泛型方法)
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架组件类型。</typeparam>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T GetComponent<T>() where T : GameFrameworkComponent
        {
            return GetComponent(typeof(T)) as T;
        }

        /// <summary>
        /// 获取游戏框架组件(通过组件类型)
        /// </summary>
        /// <param name="type">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static GameFrameworkComponent GetComponent(Type type)
        {
            var current = s_AllComponentList.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                    return current.Value;

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 获取游戏框架组件(通过组件类型名称)
        /// </summary>
        /// <param name="typeName">要获取的游戏框架组件类型名称。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static GameFrameworkComponent GetComponent(string typeName)
        {
            var current = s_AllComponentList.First;
            while (current != null)
            {
                var type = current.Value.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                    return current.Value;

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 注册游戏框架组件。
        /// </summary>
        /// <param name="gameFrameworkComponent">要注册的游戏框架组件。</param>
        internal static void RegisterComponent(GameFrameworkComponent gameFrameworkComponent)
        {
            if (!gameFrameworkComponent)
            {
                Log.Error("要注册的游戏框架组件为空.");
                return;
            }

            var type = gameFrameworkComponent.GetType();

            var current = s_AllComponentList.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    Log.Error("要注册的游戏框架组件 '{0}' 已存在.", type.FullName);
                    return;
                }

                current = current.Next;
            }

            s_AllComponentList.AddLast(gameFrameworkComponent);
        }

        /// <summary>
        /// 关闭游戏框架。退出游戏时调用
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型。</param>
        public static void Shutdown(ShutdownType shutdownType)
        {
            Log.Info("关闭游戏框架 ({0})...", shutdownType);

            var baseComponent = GetComponent<BaseComponent>();
            if (baseComponent) baseComponent.Shutdown();

            s_AllComponentList.Clear();

            switch (shutdownType)
            {
                case ShutdownType.None: return;
                case ShutdownType.Restart:
                    SceneManager.LoadScene(GameFrameworkSceneId);
                    return;
                case ShutdownType.Quit:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shutdownType), shutdownType, null);
            }
        }
    }
}