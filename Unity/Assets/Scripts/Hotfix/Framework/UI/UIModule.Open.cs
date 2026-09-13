using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Config;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI管理模块分部类之一。
    /// 目标：用于打开UI界面。
    /// 功能：
    ///     1. 异步打开UI界面。
    ///     2. 创建FairyUI界面。
    ///     3. 设置界面实例是否加锁，如果加锁，则不会被释放(销毁)。
    ///     4. 设置界面实例对象的优先级。优先级小的实例会优先被释放。
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T">界面类型。</typeparam>
        public void Open<T>(object userData = null) where T : WinBase, new()
        {
            _OpenAsync<T>(userData).Forget();
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面实例。</returns>
        public async UniTask<T> OpenAsync<T>(object userData = null) where T : WinBase, new()
        {
            return await _OpenAsync<T>(userData);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面实例；UI模块已销毁（生命周期令牌取消）时返回 null。</returns>
        private async UniTask<T> _OpenAsync<T>(object userData = null) where T : WinBase, new()
        {
            var winName = typeof(T).Name;

            // 查询 UIConfig：是否带模糊背景（与 WinBase.Init 同款查表方式）
            var uiConfig = ConfigModule.Instance?.GetConfig<TbUIConfig>()?.Get(winName);
            var needBlur = uiConfig?.Blur == true;

            // 铁律5：捕获模块生命周期令牌，并把本次打开登记为在途操作。
            // 模块销毁（OnDispose → ReleaseBlur）会取消令牌，本方法在每个 await 之后校验它：
            // 一旦取消立即回收已获取的界面实例对象、返回 null，不再继续创建/登记窗口
            // （否则续体会继续对已销毁的 m_WinObjPool 做 Register/Recycle，造成泄漏或异常）。
            // 在途登记（Begin）使框架重启时的 CancelAsync 能等到本次打开清理完毕再返回。
            var       token    = m_Scope.Token;
            using var inFlight = m_Scope.Begin();

            if (token.IsCancellationRequested)
            {
                FuLogger.LogWarning($"[UIModule] 界面 {winName} 打开中止：UI模块已销毁。");
                return null;
            }

            // 检查是否已经在加载中
            if (IsLoading(winName))
            {
                FuLogger.LogWarning($"[UIModule] 界面 {winName} 已经正在加载.");
                return null;
            }

            // 检查是否已存在该界面
            if (Has(winName))
            {
                FuLogger.LogWarning($"[UIModule] 界面 {winName} 已经存在，不能重复打开.");
                return Get<T>();
            }

            // 分配临时序列号，用于管理加载状态
            var tempSerialId = ++m_SerialId;

            // 添加到加载字典
            m_LoadingDict.TryAdd(tempSerialId, winName);

            WinObject winObj = null;
            try
            {
                // 获取界面实例对象，如果对象池中存在，则直接使用对象池中的对象
                winObj = m_WinObjPool.Spawn(winName);
                var win = winObj?.Target as T;

                // 池中实例与目标类型不符（同名不同类等）：视为无效实例，销毁后走新建流程，
                // 否则会把不属于 T 的实例当 T 使用。
                if (winObj != null && win == null)
                {
                    DestroyWinObject(winObj);
                    winObj = null;
                }

                if (winObj != null)
                {
                    // Blur=true：先截屏冻结"UI界面出现前"的画面
                    if (needBlur)
                        await OnWinOpeningAsync();

                    // 模块已销毁（token 取消）或本次加载已被 CloseAllLoading 取消：销毁已获取的界面实例对象
                    // 并中止，不再创建/登记窗口（否则 CloseAll 之后窗口仍会照常上屏）。
                    if (token.IsCancellationRequested || IsLoadingAborted(tempSerialId))
                    {
                        DestroyWinObject(winObj);
                        return null;
                    }

                    // 使用临时序列号创建Fui界面
                    return CreateFuiWin(win, tempSerialId, false, userData);
                }

                // 创建界面实例对象
                win    = new T();

                // 键统一：对象池的登记键（WinObject.Create 用 win.WinName）与查询键必须一致。
                // 上面的 Spawn 只能用类型名（生成器保证与 WinName 相同）；一旦二者不一致，
                // 池会登记在 WinName 下却按类型名查找，导致复用永久失效。故此处用真实 WinName 再查一次池。
                if (winName != win.WinName)
                {
                    winName = win.WinName;

                    var pooledObj = m_WinObjPool.Spawn(winName);
                    var pooledWin = pooledObj?.Target as T;
                    if (pooledObj != null && pooledWin == null)
                    {
                        DestroyWinObject(pooledObj);
                        pooledObj = null;
                    }

                    if (pooledObj != null)
                    {
                        // 命中真实 WinName 键的池实例：改用它（上面 new T() 仅用于读取名称，无资源持有）
                        win    = pooledWin;
                        winObj = pooledObj;

                        // Blur=true：先截屏冻结"UI界面出现前"的画面
                        if (needBlur) await OnWinOpeningAsync();

                        if (token.IsCancellationRequested || IsLoadingAborted(tempSerialId))
                        {
                            DestroyWinObject(winObj);
                            return null;
                        }

                        // 使用临时序列号创建Fui界面
                        return CreateFuiWin(win, tempSerialId, false, userData);
                    }
                }

                winObj = WinObject.Create(win.WinName, win);
                m_WinObjPool.Register(winObj, true);

                // UI包已经加载过，则直接创建Fui界面
                if (PkgManager.IsLoadedPkg(win.PackageName))
                {
                    // Blur=true：先截屏冻结"UI界面出现前"的画面
                    if (needBlur)
                        await OnWinOpeningAsync();

                    if (token.IsCancellationRequested || IsLoadingAborted(tempSerialId))
                    {
                        DestroyWinObject(winObj);
                        return null;
                    }

                    // 使用临时序列号创建Fui界面
                    return CreateFuiWin(win, tempSerialId, true, userData);
                }

                // UI包没有加载过，则等待加载UI包，加载完成后再创建Fui界面
                await PkgManager.LoadPkgAsync(win.PackageName);

                if (token.IsCancellationRequested || IsLoadingAborted(tempSerialId))
                {
                    DestroyWinObject(winObj);
                    return null;
                }

                // Blur=true：先截屏冻结"UI界面出现前"的画面
                if (needBlur) await OnWinOpeningAsync();

                if (token.IsCancellationRequested || IsLoadingAborted(tempSerialId))
                {
                    DestroyWinObject(winObj);
                    return null;
                }

                // 使用临时序列号创建Fui界面
                return CreateFuiWin(win, tempSerialId, true, userData);
            }
            catch
            {
                // 异步加载阶段失败（CreateFuiWin 之前）：彻底销毁半成品 WinObject（从池中移除 + Dispose）。
                // 不能只 Recycle 回池：未 Init 的半成品（WinUI == null）留在池中会被后续 Open 复用，
                // 拿 null 去 Init/AddChild 后再次回收，令该界面名此后每次打开都失败。
                DestroyWinObject(winObj);
                throw;
            }
            finally
            {
                // 确保从加载字典与在途取消集合中移除（本方法无论走哪条路径/是否被中止都会执行）
                m_LoadingDict.Remove(tempSerialId);
                m_CancelLoadingSet.Remove(tempSerialId);
            }
        }

        /// <summary>
        /// 本次打开的加载是否已被 CloseAllLoading 取消。
        /// </summary>
        /// <param name="tempSerialId">本次加载的临时序列号。</param>
        /// <returns>是否已被取消。</returns>
        private bool IsLoadingAborted(int tempSerialId) => m_CancelLoadingSet.Contains(tempSerialId);

        /// <summary>
        /// 创建FUI界面
        /// </summary>
        /// <param name="win">界面实例。</param>
        /// <param name="serialId">界面序列号。</param>
        /// <param name="isNewWin">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建成功返回界面实例；失败时销毁该实例并返回已存在界面或 null。</returns>
        private T CreateFuiWin<T>(T win, int serialId, bool isNewWin, object userData = null) where T : WinBase, new()
        {
            try
            {
                if (win == null) throw new InvalidOperationException($"[UIModule] 创建界面实例{typeof(T).Name}失败.");

                // 复用校验：对象池实例必须带有可用的 WinUI 才能复用（半成品/上次创建失败残留的实例
                // WinUI 为空，拿它去 AddChild 会直接抛 NRE，且会反复失败，故降级为新实例重建）。
                var reuse = !isNewWin && win.WinUI != null;

                // 创建FUI界面。复用对象时直接使用已有 WinUI，避免每次新建 FairyGUI 对象造成泄漏。
                var winUI = reuse ? win.WinUI : UIPackage.CreateObject(win.PackageName, win.WinName) as GComponent;

                // 初始化界面
                win.Init(serialId, winUI, !reuse, userData);

                // Init 内部 catch 会吞掉初始化异常，此时窗口可能已半初始化（WinUI 已赋值但成员未就绪）。
                // 不能继续 AddChild 上屏，统一抛异常走本方法既有的失败分支
                //（移除组内残留 + 销毁半成品 + 广播 OpenUIFailureEventArgs）。
                if (win.InitFailed)
                    throw new InvalidOperationException($"[UIModule] 界面 '{win.WinName}' 初始化失败，中止打开。");

                // FUI界面加入界面组
                var uiGroup = win.UIGroup;

                // AddChild会自动sort++ 
                uiGroup.AddChild(win.WinUI);
                uiGroup.Add(win);

                win._OnOpen();     // 界面打开回调
                uiGroup.Refresh(); // 刷新界面组

                // 模糊界面：显示模糊覆盖层并播放渐入
                if (win.UIConfig?.Blur == true)
                    OnWinOpened(win);

                // 广播界面打开成功事件
                var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(win, userData);
                m_EventModule.Broadcast(this, openUISuccessEventArgs);

                return win;
            }
            catch (Exception exception)
            {
                // 若失败发生在 uiGroup.Add 之后，先把界面从组内移除，再销毁实例：
                // 组内残留 WinInfo 会使 Has(winName) 恒为 true，该界面名此后每次打开都被判为"已经存在"。
                RemoveWinFromGroup(win);

                // 打开失败：彻底销毁该界面实例对象（从池中移除 + Dispose），避免半成品留在池中被后续打开复用
                DestroyWinObject(win);

                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(serialId, typeof(T).Name, userData);
                m_EventModule.Broadcast(this, openUIFailureEventArgs);
                FuLogger.LogError($"[UIModule] 打开UI界面失败, 资源名称 '{typeof(T).Name}', 错误信息 '{exception}'.");
                return Get(serialId) as T;
            }
        }

        /// <summary>
        /// 打开失败时把界面从所属界面组中移除（未加入组时不做处理）。
        /// 失败若发生在 uiGroup.Add(win) 之后，组内会残留 WinInfo，使 Has(winName) 恒为 true，
        /// 该界面名此后每次打开都会因"已经存在"而直接返回，永久打不开。
        /// </summary>
        /// <param name="win">界面实例，可为 null。</param>
        private void RemoveWinFromGroup(WinBase win)
        {
            if (win == null) return;

            try
            {
                var uiGroup = win.UIGroup;
                if (uiGroup == null || !uiGroup.Has(win.SerialId)) return;

                uiGroup.Remove(win);
                uiGroup.Refresh(); // 同步剩余界面的覆盖/暂停状态（原先该界面在 Refresh 之前失败时会漏刷）
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"[UIModule] 打开失败时从界面组移除界面 '{win.WinName}' 出现异常: {e.Message}");
            }
        }

        /// <summary>
        /// 彻底销毁界面实例（从对象池中移除并 Dispose），供打开失败路径清理半成品实例。
        /// 失败路径不得只 Recycle 回池：半成品实例（未 Init，WinUI == null）留在池中会被后续打开复用，
        /// 拿 null 去 Init/AddChild 并再次回收，令该界面名此后每次打开都失败，故必须销毁而非回收。
        /// </summary>
        /// <param name="target">界面实例对象（WinObject）或界面实例（WinBase），为 null 时不做处理。</param>
        private void DestroyWinObject(object target)
        {
            if (target == null) return;

            // 先回收清除"使用中"计数：DisposeObject 对使用中的对象直接返回 false，只解引用会残留池槽
            try
            {
                m_WinObjPool.TryRecycle(target);
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"[UIModule] 销毁池中界面实例前回收失败: {e.Message}");
            }

            try
            {
                // Dispose 内部经 WinObject.OnDispose 销毁 WinUI 并触发 WinBase._OnDispose（半成品时其内部已判空）
                if (!m_WinObjPool.DisposeObject(target))
                    FuLogger.LogWarning("[UIModule] 销毁池中界面实例未成功（可能已被移除）。");
            }
            catch (Exception e)
            {
                FuLogger.LogWarning($"[UIModule] 销毁池中界面实例出现异常: {e.Message}");
            }
        }

        /// <summary>
        /// 设置界面实例是否加锁，如果加锁，则不会被释放(销毁)。
        /// </summary>
        /// <param name="winUI">要设置是否加锁的界面实例。</param>
        /// <param name="locked">界面实例是否加锁。</param>
        public void SetUILocked(object winUI, bool locked) => m_WinObjPool.SetLocked(winUI, locked);

        /// <summary>
        /// 设置界面实例对象的优先级。优先级小的实例会优先被释放。
        /// </summary>
        /// <param name="winUI">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIPriority(object winUI, int priority) => m_WinObjPool.SetPriority(winUI, priority);
    }
}