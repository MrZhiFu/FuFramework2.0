using System;
using System.Collections.Generic;
using GameFrameX.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架入口。
    /// </summary>
    public static class GameFrameworkEntry
    {
        /// <summary>
        /// 所有游戏框架模块。
        /// </summary>
        private static readonly GameFrameworkLinkedList<GameFrameworkModule> s_AllModuleList = new();

        /// <summary>
        /// 所有游戏框架模块类型映射字典，key：为游戏框架模块接口类型如各个IxxxManager，value：为游戏框架模块具体类型，如xxxManager。
        /// </summary>
        private static readonly Dictionary<Type, Type> s_ModuleTypeDict = new();

        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var module in s_AllModuleList)
            {
                module.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理所有游戏框架模块。
        /// </summary>
        public static void Shutdown()
        {
            for (var current = s_AllModuleList.Last; current != null; current = current.Previous)
            {
                current.Value.Shutdown();
            }

            s_AllModuleList.Clear();
            ReferencePool.ClearAll();
            GameFrameworkLog.SetLogHelper(null);
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架模块类型。</typeparam>
        /// <returns>要获取的游戏框架模块。</returns>
        /// <remarks>如果要获取的游戏框架模块不存在，则自动创建该游戏框架模块。</remarks>
        public static T GetModule<T>() where T : class
        {
            var interfaceType = typeof(T);

            if (!interfaceType.IsInterface)
                throw new GameFrameworkException(Utility.Text.Format("要获取框架模块必须为接口类型, 但是 '{0}' 不是接口类型.", interfaceType.FullName));

            // if (interfaceType.FullName != null && !interfaceType.FullName.StartsWith("FuFramework.", StringComparison.Ordinal))
            //     throw new GameFrameworkException(Utility.Text.Format("要获取的框架模块必须是命名空间为FuFramework的模块, 但是 '{0}' 不是FuFramework模块.", interfaceType.FullName));

            // 如GameFramework.Resource.IResourceManager => GameFramework.Resource.ResourceManager
            if (s_ModuleTypeDict.TryGetValue(interfaceType, out var moduleType))
                return GetModule(moduleType) as T;

            var moduleName = Utility.Text.Format("{0}.{1}", interfaceType.Namespace, interfaceType.Name.Substring(1));
            moduleType = s_ModuleTypeDict.TryGetValue(interfaceType, out moduleType) ? moduleType : Type.GetType(moduleName);

            if (moduleType == null)
                throw new GameFrameworkException(Utility.Text.Format("在FuFramework中找不到模块 '{0}''.", moduleName));

            return GetModule(moduleType) as T;
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要获取的游戏框架模块类型。</param>
        /// <returns>要获取的游戏框架模块。</returns>
        /// <remarks>如果要获取的游戏框架模块不存在，则自动创建该游戏框架模块。</remarks>
        public static GameFrameworkModule GetModule(Type moduleType)
        {
            foreach (var module in s_AllModuleList)
            {
                if (module.GetType() != moduleType) continue;
                return module;
            }

            return CreateModule(moduleType);
        }

        /// <summary>
        /// 注册框架模块。
        /// </summary>
        /// <param name="interfaceType"> 要注册的游戏框架模块接口类型。</param>
        /// <param name="implType"> 要注册的游戏框架模块类型。</param>
        /// <returns>要创建的游戏框架模块。</returns>
        public static void RegisterModule(Type interfaceType, Type implType)
        {
            if (!interfaceType.IsInterface)
                throw new GameFrameworkException(Utility.Text.Format("要注册的框架模块必须为接口类型, 但是 '{0}' 不是接口类型.", interfaceType.FullName));

            if (!implType.IsClass || implType.IsInterface || implType.IsAbstract)
                throw new GameFrameworkException(Utility.Text.Format("要注册的框架模块必须为非抽象类, 但是 '{0}' 不是非抽象类.", implType.FullName));

            if (!s_ModuleTypeDict.TryGetValue(interfaceType, out _))
            {
                s_ModuleTypeDict[interfaceType] = implType;
            }
        }

        /// <summary>
        /// 创建游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要创建的游戏框架模块类型。</param>
        /// <returns>要创建的游戏框架模块。</returns>
        private static GameFrameworkModule CreateModule(Type moduleType)
        {
            var module = (GameFrameworkModule)Activator.CreateInstance(moduleType);
            if (module == null) throw new GameFrameworkException(Utility.Text.Format("创建模块失败, 模块类型 '{0}'.", moduleType.FullName));

            var current = s_AllModuleList.First;
            while (current != null)
            {
                if (module.Priority > current.Value.Priority) break;
                current = current.Next;
            }

            if (current != null)
                s_AllModuleList.AddBefore(current, module);
            else
                s_AllModuleList.AddLast(module);

            return module;
        }
    }
}