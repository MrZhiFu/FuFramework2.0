using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源加载注册器，实现 ICancelAsync（可取消异步对象）。
    /// 功能：
    ///     1.异步加载资源。
    ///     2.记录加载过的资源句柄，避免重复加载。
    ///     3.异步实例化游戏物体。
    ///     4.卸载资源。
    /// 作为「逻辑分组的资源装载器」使用（如 UI 包：包打开时加载、包关闭时 UnloadAll 整组释放）。
    /// 每个实例归单一调用方持有，用完 UnloadAll（临时卸载，装载器可复用）或 Dispose（永久废弃，在途加载随之取消）后弃引用由 GC 回收。
    /// 装载器自身即异步链的生命周期所有者：内部透传 m_Scope.Token 给 AssetModule，不接收调用方 token（整组加载/整组释放模型）。
    /// </summary>
    public class AssetLoadRegister : ICancelAsync
    {
        /// <summary>
        /// 资源管理模块（实例持有，构造时获取）。
        /// </summary>
        private readonly AssetModule m_AssetModule;

        /// <summary>
        /// 取消范围：Dispose 时 Cancel，在途加载随装载器销毁而取消；UnloadAll（临时卸载）不取消，装载器可复用。
        /// 业务可 await CancelAsync 等待在途加载清理完毕。
        /// </summary>
        private readonly CancellationScope m_Scope = new();

        /// <summary>
        /// 缓存已经加载的资源句柄，key 为资源路径 + 类型，value 为资源句柄。
        /// </summary>
        private readonly Dictionary<LoadKey, AssetHandle> m_HandleDict = new();

        /// <summary>
        /// 正在加载中的任务去重字典，key为资源路径+类型，value为共享完成源。
        /// 同一路径+类型并发加载时共享完成源（UniTaskCompletionSource.Task 可被多个调用者 await），
        /// 防止后完成句柄覆盖先完成句柄导致泄漏，也避免 async UniTask 重复 await 报错。
        /// </summary>
        private readonly Dictionary<LoadKey, UniTaskCompletionSource<AssetHandle>> m_LoadingTasks = new();

        /// <summary>
        /// 是否已废弃（调用方永久放弃本装载器）。防止废弃后在途的加载任务把句柄写回缓存（句柄将无人释放而泄漏）。
        /// </summary>
        private bool m_Disposed;

        /// <summary>
        /// 是否处于临时卸载状态（UnloadAll 置位）。防止"卸载后、复用前"的在途加载把句柄重新缓存（ref→0 内存不释放）。
        /// 与 m_Disposed 的区别：UnloadAll 后装载器仍可复用（新加载请求会清除此标记）；Dispose 永久废弃不可复用。
        /// </summary>
        private bool m_Unloaded;

        /// <summary>
        /// 取消令牌：装载器永久废弃（Dispose）后触发，拒绝新的加载请求。
        /// 注：在途加载的真正取消由 AssetModule 自身的 Token 承担（模块加载内部观察），本 Token 是取消清理/准入代理——
        /// 业务可 await CancelAsync 等待在途加载清理完毕；永久废弃装载器请先 Dispose 再 await CancelAsync。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途加载完成清理（释放句柄 + 卸载资源）后才返回。可重入、幂等。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();

        /// <summary>
        /// 资源加载注册器。纯实例类：每次 new 创建独立装载器，调用方持有，用完 UnloadAll/Dispose 后弃引用由 GC 回收。
        /// </summary>
        public AssetLoadRegister()
        {
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <returns>加载的资源对象；类型不匹配时释放句柄并抛异常。</returns>
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            var handle = await LoadAssetHandleAsync(path, typeof(T));
            var result = handle.GetAssetObject<T>();
            if (result == null)
            {
                // 类型不匹配：失效该 (path,type) 缓存并释放句柄，避免错误类型句柄永久滞留缓存（资源被钉死无法卸载）。
                // 仅影响该类型条目，不影响同路径其他类型条目（缓存按 path+type 分 key）。
                m_HandleDict.Remove(new LoadKey(path, typeof(T)));
                handle.Release();
                m_AssetModule.UnloadAsset(path);
                throw new InvalidOperationException($"[AssetLoadRegister]资源{path}类型不匹配，期望类型: {typeof(T)}");
            }

            return result;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <param name="type">资源类型。</param>
        /// <returns>加载的资源对象。</returns>
        public async UniTask<Object> LoadAsync(string path, Type type)
        {
            var handle = await LoadAssetHandleAsync(path, type);
            return handle.AssetObject;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <returns>加载的资源对象。</returns>
        public async UniTask<Object> LoadAsync(string path)
        {
            var handle = await LoadAssetHandleAsync(path, null);
            return handle.AssetObject;
        }

        /// <summary>
        /// 异步实例化游戏物体。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>实例化后的实体。</returns>
        public async UniTask<GameObject> InstantiateAsync(string path)
        {
            var assetHandle          = await LoadAssetHandleAsync(path, null);
            var instantiateOperation = assetHandle.InstantiateAsync();
            await instantiateOperation;
            if (instantiateOperation.Result == null)
                throw new InvalidOperationException($"[AssetLoadRegister]实例化资源{path}失败（资源可能不是 GameObject 或加载异常）");
            return instantiateOperation.Result;
        }

        /// <summary>
        /// 加载资源句柄的通用逻辑。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <param name="assetType">资源类型，null 表示不指定类型。</param>
        /// <returns>资源句柄。</returns>
        private async UniTask<AssetHandle> LoadAssetHandleAsync(string path, Type assetType)
        {
            // 已废弃（Dispose）后直接拒绝新加载，避免发起真实加载后在核心检测到 m_Disposed 才释放（浪费性加载且语义混乱）
            if (m_Disposed) throw new ObjectDisposedException(nameof(AssetLoadRegister));

            // 新的加载请求意味着装载器正在被再次使用（如复用），清除临时卸载标记
            m_Unloaded = false;

            var key = new LoadKey(path, assetType);

            // 检查是否已加载
            if (m_HandleDict.TryGetValue(key, out var existingHandle))
            {
                // 验证资源是否仍然有效
                if (existingHandle.AssetObject != null)
                    return existingHandle;

                // 资源已失效，清理后重新加载
                m_HandleDict.Remove(key);
                existingHandle.Release();
            }

            // 并发去重：同一路径+类型正在加载，共享完成源（UniTaskCompletionSource.Task 可多次 await）
            if (m_LoadingTasks.TryGetValue(key, out var sharedSource))
            {
                var sharedHandle = await sharedSource.Task;
                // 等待期间可能被 Dispose/UnloadAll：句柄可能已被释放，不得返回给调用方
                if (m_Disposed || m_Unloaded)
                {
                    sharedHandle?.Release();
                    throw new ObjectDisposedException($"{nameof(AssetLoadRegister)}已废弃或已卸载");
                }

                return sharedHandle;
            }

            // 创建共享完成源并注册，多个并发调用者 await 同一 Task
            var taskSource = new UniTaskCompletionSource<AssetHandle>();
            m_LoadingTasks[key] = taskSource;
            try
            {
                var handle = await LoadAssetHandleCoreAsync(path, assetType);
                taskSource.TrySetResult(handle);
                return handle;
            }
            catch (Exception e)
            {
                taskSource.TrySetException(e); // 失败：所有 await 此 Task 的调用者抛异常
                throw;
            }
            finally
            {
                m_LoadingTasks.Remove(key);
            }
        }

        /// <summary>
        /// 实际加载资源句柄并缓存。并发请求经 LoadAssetHandleAsync 去重后共享此任务。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <param name="assetType">资源类型，null 表示不指定类型。</param>
        /// <returns>资源句柄。</returns>
        private async UniTask<AssetHandle> LoadAssetHandleCoreAsync(string path, Type assetType)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：Token 取消（Dispose/CancelAsync）后拒绝新加载

            using (m_Scope.Begin()) // 登记在途：Dispose 取消时等待本操作清理完毕
            {
                AssetHandle assetHandle = null;
                try
                {
                    assetHandle = assetType == null
                        ? await m_AssetModule.LoadAssetAsync(path, m_Scope.Token)
                        : await m_AssetModule.LoadAssetAsync(path, assetType, m_Scope.Token);
                    if (assetHandle == null || assetHandle.AssetObject == null)
                    {
                        throw new InvalidOperationException($"[AssetLoadRegister]资源{path}加载失败");
                    }

                    // 加载期间装载器已被废弃（Dispose）或处于临时卸载（UnloadAll 且无新加载接管）：
                    // 句柄已不归本实例，释放并阻止写回（否则句柄无人释放而泄漏 / ref→0 后资源被重新缓存）
                    if (m_Disposed || m_Unloaded)
                    {
                        assetHandle.Release();
                        // 补 UnloadAsset：仅 Release 在 AutoUnloadBundleWhenUnused=false 下不会卸载 bundle，
                        // 若该资源仅由本次被中止的加载加载过、装载器后续不再加载它，bundle 将永久残留；
                        // 已释放后 TryUnloadUnusedAsset 对仍被其他系统持有的句柄（引用计数 >0）安全跳过，共享无虞
                        m_AssetModule.UnloadAsset(path);
                        assetHandle = null; // 置空避免 catch 二次释放
                        throw new ObjectDisposedException($"{nameof(AssetLoadRegister)}已废弃或已卸载");
                    }

                    // 保存资源句柄
                    m_HandleDict[new LoadKey(path, assetType)] = assetHandle;
                    FuLogger.LogInfo($"[AssetLoadRegister]加载{path}资源完成");
                    return assetHandle;
                }
                catch
                {
                    // 失败：释放句柄并重抛；m_Disposed/m_Unloaded 分支已自行释放并置空，此处不会二次释放
                    assetHandle?.Release();
                    throw;
                }
            }
        }

        /// <summary>
        /// 卸载已经加载的指定资源。
        /// 1.释放资源句柄，即减少引用计数。
        /// 2.尝试卸载资源，即引用计数为零时，才会真正卸载资源。
        /// 注意：若该资源仍在加载中（尚未入缓存），本方法不生效，需在加载完成后调用；
        /// 若要阻止在途加载完成时写回缓存，请用 UnloadAll（置 m_Unloaded）或 Dispose。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public void Unload(string path)
        {
            // 同一路径可能以多种类型缓存（按 path+type 分 key），全部卸载
            List<LoadKey> matchedKeys = null;
            foreach (var key in m_HandleDict.Keys)
            {
                if (key.Path == path)
                {
                    matchedKeys ??= new List<LoadKey>();
                    matchedKeys.Add(key);
                }
            }

            if (matchedKeys == null) return;
            foreach (var key in matchedKeys)
            {
                if (!m_HandleDict.TryGetValue(key, out var handle)) continue;

                // 释放资源句柄，即减少引用计数
                handle.Release();

                // 尝试卸载资源，即引用计数为零时，才会真正卸载资源
                m_AssetModule.UnloadAsset(path);

                m_HandleDict.Remove(key);
                FuLogger.LogInfo($"[AssetLoadRegister]释放{path}资源完成.");
            }
        }

        /// <summary>
        /// 卸载所有已经加载的资源（临时卸载：装载器保留可复用）。
        /// 置 m_Unloaded 标记：卸载后在途的加载完成时不得写回缓存（防 ref→0 后资源句柄被重新缓存、内存不释放）。
        /// </summary>
        public void UnloadAll()
        {
            m_Unloaded = true;

            // 先复制 key 列表，避免遍历时集合被修改；直接按 key 卸载（比逐个 Unload 再按 path 扫字典更省）
            var keys = new List<LoadKey>(m_HandleDict.Keys);
            foreach (var key in keys)
            {
                if (!m_HandleDict.TryGetValue(key, out var handle)) continue;

                // 释放资源句柄，即减少引用计数
                handle.Release();

                // 尝试卸载资源，即引用计数为零时，才会真正卸载资源
                m_AssetModule.UnloadAsset(key.Path);

                m_HandleDict.Remove(key);
                FuLogger.LogInfo($"[AssetLoadRegister]释放{key.Path}资源完成.");
            }
        }

        /// <summary>
        /// 废弃装载器：调用方永久放弃，释放全部句柄并标记废弃。
        /// 在途加载任务完成时会检查 m_Disposed 中止（释放句柄、不写回缓存），避免句柄无人释放而泄漏。
        /// 与 UnloadAll 的区别：UnloadAll 用于临时释放（调用方仍会复用本装载器），Dispose 用于永久丢弃。
        /// </summary>
        public void Dispose()
        {
            m_Scope.Cancel(); // 永久废弃：取消在途加载
            m_Disposed = true;
            UnloadAll();
            m_LoadingTasks.Clear();
        }
    }
}