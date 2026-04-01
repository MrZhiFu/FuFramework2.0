using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 框架模块管理器。
    /// 管理(注册和获取)框架中的所有组件。
    /// 具备循环依赖检测功能，能够防止模块注册过程中出现无限递归导致的栈溢出问题
    /// </summary>
    public static class ModuleManager
    {
        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        private const int GameFrameworkSceneId = 0;

        /// <summary>
        /// 记录所有已注册的模块的链表集合
        /// </summary>
        private static readonly FuLinkedList<FuModule> ModuleList = new();

        /// <summary>
        /// 记录正在注册的模块类型，用于检测循环依赖
        /// </summary>
        private static readonly HashSet<Type> RegisteringSet = new();

        /// <summary>
        /// 模块缓存的字典，key:模块类型，value:模块对象。
        /// </summary>
        private static readonly Dictionary<Type, FuModule> ModuleCacheDict = new(30);

        /// <summary>
        /// 模块根节点
        /// </summary>
        private static Transform m_ModuleRoot;

        /// <summary>
        /// 获取模块根节点
        /// </summary>
        private static Transform ModuleRoot
        {
            get
            {
                if (m_ModuleRoot is not null)
                    return m_ModuleRoot;

                var rootObj = new GameObject("[FrameworkModule]");
                Object.DontDestroyOnLoad(rootObj);
                m_ModuleRoot = rootObj.transform;
                return m_ModuleRoot;
            }
        }

        /// <summary>
        /// 初始化游戏框架。
        /// </summary>
        public static void Initialize()
        {
            // 自动注册所有框架模块
            RegisterAllModules();
        }

        /// <summary>
        /// 框架模块轮询
        /// </summary>
        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var module in ModuleList)
            {
                if (!module.IsInitialized) continue;
                module.OnUpdate(deltaTime, unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 关闭游戏框架，退出游戏时调用。如设置界面的重启/退出按钮。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型。</param>
        public static void Shutdown(ShutdownType shutdownType)
        {
            FuLogger.LogInfo($"<color=#FF6B6B>=====开始关闭框架，类型: {shutdownType}，需要关闭的模块数量: {ModuleList.Count} ====</color>");

            // 使用栈来存储需要关闭的模块（逆序关闭）
            var shutdownStack = new Stack<FuModule>();

            // 将模块按关闭顺序压入栈中
            var current = ModuleList.First;
            while (current != null)
            {
                if (current.Value.IsInitialized)
                {
                    shutdownStack.Push(current.Value);
                }

                current = current.Next;
            }

            // 从栈中弹出并关闭模块（逆序关闭）
            while (shutdownStack.Count > 0)
            {
                var module = shutdownStack.Pop();
                try
                {
                    FuLogger.LogInfo($"<color=#00FBD5>关闭模块: {module.GetType().Name}</color>");
                    module.OnDispose();
                    module.IsInitialized = false;
                }
                catch (Exception e)
                {
                    FuLogger.LogError($"模块 {module.GetType().Name} 关闭失败: {e.Message}");
                    FuLogger.LogError($"异常类型: {e.GetType().Name}");
                    FuLogger.LogError($"堆栈跟踪: {e.StackTrace}");
                }
            }

            // 清空管理器的集合
            ModuleList.Clear();
            ModuleCacheDict.Clear();
            RegisteringSet.Clear();

            // 执行具体的关闭操作
            ExecuteShutdown(shutdownType);
        }

        /// <summary>
        /// 获取游戏框架模块，如果不存在则自动注册
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架组件类型。</typeparam>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T GetModule<T>() where T : FuModule => GetModule(typeof(T)) as T;

        /// <summary>
        /// 获取游戏框架组件
        /// </summary>
        /// <param name="type">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static FuModule GetModule(Type type)
        {
            // 先从缓存查找
            if (ModuleCacheDict.TryGetValue(type, out var cachedModule))
                return cachedModule;

            // 从链表查找
            var current = ModuleList.First;
            while (current is not null)
            {
                var module = current.Value;
                if (module.GetType() == type)
                {
                    ModuleCacheDict[type] = module;
                    return module;
                }

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 获取所有已注册的模块。
        /// </summary>
        /// <returns>模块列表。</returns>
        public static List<FuModule> GetAllModules() => ModuleList.ToList();

        /// <summary>
        /// 注册游戏框架模块
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RegisterModule<T>() where T : FuModule => RegisterModule(typeof(T)) as T;

        /// <summary>
        /// 注册游戏框架模块
        /// </summary>
        /// <param name="moduleType">要注册的模块类型。</param>
        /// <returns>注册的模块实例。</returns>
        public static FuModule RegisterModule(Type moduleType)
        {
            if (!typeof(FuModule).IsAssignableFrom(moduleType))
            {
                FuLogger.LogError($"类型 {moduleType.Name} 不是有效的FuModule类型!");
                return null;
            }

            try
            {
                // 检查模块是否已存在
                var module = GetModule(moduleType);
                if (module is not null) return module;

                // 添加到正在注册集合，防止循环依赖
                RegisteringSet.Add(moduleType);

                try
                {
                    // 检查模块依赖
                    CheckModuleDependencies(moduleType);

                    // 查找现有模块或创建新模块
                    module = Object.FindObjectOfType(moduleType, true) as FuModule;
                    if (module is null)
                    {
                        // 创建模块的GameObject
                        var moduleObject = new GameObject();
                        module            = moduleObject.AddComponent(moduleType) as FuModule;
                        moduleObject.name = $"[Module]-{moduleType.Name}";

                        // 确保框架模块在场景切换时不被销毁
                        Object.DontDestroyOnLoad(moduleObject);

                        // 设置父对象到框架根节点
                        moduleObject.transform.SetParent(ModuleRoot);
                    }

                    if (module is null)
                    {
                        FuLogger.LogError($"注册模块 {moduleType.Name} 失败: 无法创建模块组件!");
                        return null;
                    }

                    // 注册模块到链表和缓存中
                    RegisterModuleInternal(module);

                    return module;
                }
                finally
                {
                    // 从正在注册集合中移除
                    RegisteringSet.Remove(moduleType);
                }
            }
            catch (Exception e)
            {
                FuLogger.LogError($"注册模块 {moduleType.Name} 失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查找并注册所有继承于FuModule的模块
        /// </summary>
        public static void RegisterAllModules()
        {
            FuLogger.LogInfo("<color=#00FBD5>------开始自动注册所有框架模块------</color>");

            try
            {
                // 获取所有继承自FuModule的类型
                var moduleTypes = GetAllFuModuleTypes();

                if (moduleTypes.Count == 0)
                {
                    FuLogger.LogWarning("未找到任何继承自FuModule的类型");
                    return;
                }

                FuLogger.LogInfo($"找到<color=#00FBD5> {moduleTypes.Count} </color>个框架模块");

                foreach (var fuModuleType in moduleTypes)
                {
                    RegisterModule(fuModuleType);
                }

                FuLogger.LogInfo("<color=#00FBD5>------所有模块注册完成------</color>");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"查找并注册所有继承于FuModule的模块失败: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 卸载指定模块。
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public static void UnregisterModule<T>() where T : FuModule
        {
            var module = GetModule<T>();
            if (module is null) return;

            try
            {
                if (module.IsInitialized)
                {
                    module.OnDispose();
                    module.IsInitialized = false;
                }

                // 从链表中移除
                var node = ModuleList.Find(module);
                if (node is not null)
                {
                    ModuleList.Remove(node);
                }

                // 从缓存中移除
                ModuleCacheDict.Remove(typeof(T));

                // 销毁GameObject
                if (Application.isPlaying)
                    Object.Destroy(module.gameObject);
                else
                    Object.DestroyImmediate(module.gameObject);

                FuLogger.LogInfo($"<color=#00FBD5>------卸载模块: {typeof(T).Name}</color>");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"卸载模块 {typeof(T).Name} 失败: {e.Message}");
            }
        }

        /// <summary>
        /// 检查模块依赖。
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        private static void CheckModuleDependencies(Type moduleType)
        {
            var dependencies = moduleType.GetCustomAttributes(typeof(ModuleDependencyAttribute), true);
            foreach (ModuleDependencyAttribute dependency in dependencies)
            {
                foreach (var depType in dependency.DependentTypes)
                {
                    if (!typeof(FuModule).IsAssignableFrom(depType))
                    {
                        FuLogger.LogError($"框架模块 {moduleType.Name} 的依赖 {depType.Name} 不是有效的FuModule类型!");
                        continue;
                    }

                    if (GetModule(depType) is null)
                    {
                        FuLogger.LogInfo($"框架模块 {moduleType.Name} 依赖 {depType.Name}，自动注册依赖模块...");
                        RegisterModule(depType);
                    }
                }
            }
        }

        /// <summary>
        /// 内部注册模块。
        /// </summary>
        /// <param name="module">要注册的模块。</param>
        private static void RegisterModuleInternal(FuModule module)
        {
            // 优先级大的组件注册在链表的前面
            var current = ModuleList.First;
            while (current is not null)
            {
                if (module.Priority > current.Value.Priority) break;
                current = current.Next;
            }

            if (current is not null)
                ModuleList.AddBefore(current, module);
            else
                ModuleList.AddLast(module);

            // 添加到缓存
            ModuleCacheDict[module.GetType()] = module;

            // 如果模块没有初始化过，立即初始化新模块
            if (module is { IsInitialized: false } && Application.isPlaying)
            {
                module.OnInit();
                module.IsInitialized = true;
            }

            FuLogger.LogInfo($"<color=#00FBD5>注册框架模块:{module.gameObject.name}, 优先级:{module.Priority}</color>");
        }

        /// <summary>
        /// 获取依赖链字符串，用于循环依赖错误提示
        /// </summary>
        /// <param name="currentType">当前正在注册的类型</param>
        /// <returns>依赖链字符串</returns>
        private static string GetDependencyChainString(Type currentType)
        {
            var chain = string.Join(" → ", RegisteringSet.Select(t => t.Name));
            return $"{chain} → {currentType.Name}";
        }

        /// <summary>
        /// 获取所有继承自FuModule的类型
        /// </summary>
        private static List<Type> GetAllFuModuleTypes()
        {
            var fuModuleType = typeof(FuModule);
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly =>
                                    {
                                        try
                                        {
                                            return assembly.GetTypes();
                                        }
                                        catch
                                        {
                                            return Array.Empty<Type>();
                                        }
                                    })
                                    .Where(type => type != fuModuleType                &&
                                                   fuModuleType.IsAssignableFrom(type) &&
                                                   !type.IsAbstract                    &&
                                                   !type.IsInterface)
                                    .ToList();

            return allTypes;
        }


        /// <summary>
        /// 执行具体的关闭操作
        /// </summary>
        private static void ExecuteShutdown(ShutdownType shutdownType)
        {
            switch (shutdownType)
            {
                case ShutdownType.Restart:
                    SceneManager.LoadScene(GameFrameworkSceneId);
                    break;
                case ShutdownType.Quit:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
                case ShutdownType.Unregister:
                    // 不需要额外操作
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shutdownType), shutdownType, null);
            }
        }
    }
}