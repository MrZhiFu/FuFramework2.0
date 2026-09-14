using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Config;
using Hotfix.Game.Config;
using GuideData = Hotfix.Game.Config.Guide;
using GuideStepData = Hotfix.Game.Config.GuideStep;
using AOT.Framework.Core.Log;
using UnityEngine;

namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// 引导管理模块。
    /// 功能：
    ///     1. 从配置表加载引导步骤信息。
    ///     2. 执行/跳转/取消引导步骤。
    ///     3. 缓存完成的引导。
    ///     4. 提供引导相关事件。
    /// 对外公共 API 见 GuideModule.API.cs（partial 分部类）。
    /// </summary>
    public partial class GuideModule : ModuleBase, ICancelAsync
    {
        #region 私有字段

        /// <summary>
        /// 引导动作执行器（公开属性 <see cref="GuideAction"/> 的后备字段）
        /// </summary>
        private IGuideAction m_GuideAction;

        /// <summary>
        /// 引导动作执行器的可取消异步视图（若其实现 ICancelAsync）。
        /// 生命周期：随 <see cref="GuideAction"/> 赋值捕获；OnDispose 置空 GuideAction 后仍保留本引用——
        /// 框架重启的 ModuleManager.CancelAllAsync 必须在 OnDispose 之后经它等待两条引导异步链排水完毕。
        /// </summary>
        private ICancelAsync m_GuideActionCancellable;

        /// <summary>
        /// 引导存档是否有未落盘的改动（见 OnPerSecondUpdate）
        /// </summary>
        private bool m_GuideDataDirty;

        /// <summary>
        /// 当前引导
        /// </summary>
        private GuideData m_CurrentGuide;

        /// <summary>
        /// 当前引导中的当前步骤
        /// </summary>
        private BaseStep m_CurrentStep;

        /// <summary>
        /// 引导数据字典，key 为引导 ID
        /// </summary>
        private Dictionary<int, GuideData> m_GuideDict;

        /// <summary>
        /// 步骤数据字典（来自配置表），key 为步骤 ID
        /// </summary>
        private Dictionary<int, GuideStepData> m_StepDataDict;

        /// <summary>
        /// 当前引导中的所有步骤，key为步骤Id，Value为步骤对象
        /// </summary>
        private readonly Dictionary<int, BaseStep> m_AllStepDict = new();

        /// <summary>
        /// 步骤历史记录栈。
        /// 只记录步骤 ID：步骤实例推进后会被回收，存实例会拿到已被 Clear 的死对象，故回退时按 ID 重新取回。
        /// </summary>
        private readonly Stack<int> m_StepHistoryStack = new();

        /// <summary>
        /// 缓存完成的引导，key为引导ID，Value为是否完成
        /// </summary>
        private readonly Dictionary<int, bool> m_GuideCompletionCacheDict = new();

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;

            m_GuideCompletionCacheDict.Clear();

            var tbGuide = ConfigModule.Instance?.GetConfig<TbGuide>();
            var tbGuideStep = ConfigModule.Instance?.GetConfig<TbGuideStep>();
            if (tbGuide == null || tbGuideStep == null)
            {
                FuLogger.LogError("[GuideModule] 引导配置表不存在，跳过初始化.");
                return;
            }

            m_GuideDict = new Dictionary<int, GuideData>();
            foreach (var guide in tbGuide.All)
            {
                if (m_GuideDict.ContainsKey(guide.Id))
                {
                    FuLogger.LogError($"[GuideModule] 重复的引导 ID: {guide.Id}");
                    continue;
                }

                m_GuideDict[guide.Id] = guide;
            }

            m_StepDataDict = new Dictionary<int, GuideStepData>();
            foreach (var step in tbGuideStep.All)
            {
                if (m_StepDataDict.ContainsKey(step.Id))
                {
                    FuLogger.LogError($"[GuideModule] 重复的步骤 ID: {step.Id}");
                    continue;
                }

                m_StepDataDict[step.Id] = step;
            }

            FuLogger.LogInfo($"[GuideModule] 引导管理模块初始化完成. 引导数量: {m_GuideDict.Count}, 步骤总数量: {m_StepDataDict.Count}");
        }

        /// <summary>
        /// 帧更新。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            var step = m_CurrentStep;
            if (step == null) return;

            if (step.IsExecuting)
                step.Update(deltaTime);

            // 末步收尾（唯一权威推进点）：
            // BaseStep.Complete() 只在存在 NextStepId 时经 JumpToStep 推进；末步（无 NextStepId）完成后
            // 无人调用 MoveToNextStep → FinishGuide，引导会永久停留在 Completed 状态——OnGuideFinished 与
            // MarkGuideAsCompleted 永不触发、下次启动重放整条引导（ClickUIStep/WaitStep/DefaultStep 直接调
            // Complete()，都不经过 CompleteCurrentStep，故必须在此兜底收尾）。
            // 条件用「仍是被更新过的那一步」判定：若 Update 内部已经 Complete 并推进（m_CurrentStep 已换），
            // ReferenceEquals 不成立，不会重复推进；仅当步骤常驻未推进时才收尾。MoveToNextStep 无 Next 即 FinishGuide，
            // 回收仍由 ClearGuideData 单点负责。
            if (ReferenceEquals(m_CurrentStep, step) && step.IsCompleted)
                MoveToNextStep();
        }

        /// <summary>
        /// 每秒更新：把引导存档的脏位合并落盘（见 MarkGuideAsCompleted）。
        /// </summary>
        protected internal override void OnPerSecondUpdate()
        {
            FlushGuideData();
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            // 中断当前引导
            InterruptGuide();

            // 落盘未提交的引导存档（见 MarkGuideAsCompleted：完成/中断路径只标脏，不帧内同步落盘）
            FlushGuideData();

            // 回收当前引导的所有步骤到引用池中
            foreach (var (_, step) in m_AllStepDict)
            {
                ReferencePool.Recycle(step);
            }

            m_AllStepDict.Clear();
            m_StepHistoryStack.Clear();
            m_GuideCompletionCacheDict.Clear();
            m_GuideDict?.Clear();
            m_StepDataDict?.Clear();
            m_CurrentGuide = null;

            // 清理事件订阅
            OnGuideStarted     = null;
            OnGuideFinished    = null;
            OnStepChanged      = null;
            OnGuideInterrupted = null;
            OnStepExecuting    = null;
            OnStepCompleted    = null;

            // 释放引导动作持有者（其内部持有 LifecycleCancellationSource 与模块级取消范围，需随模块销毁释放）
            (m_GuideAction as IDisposable)?.Dispose();

            // 这里刻意直接置后备字段而不走 GuideAction 属性 setter：setter 会一并清空
            // m_GuideActionCancellable，而框架重启（DisposeModules → CancelAllAsync）必须在 OnDispose 之后
            // 仍能经它排水等待两条引导异步链清理完毕。该引用由下一次 GuideAction 赋值覆盖。
            m_GuideAction = null;

            Instance = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 开始引导流程(通过 Guide)
        /// </summary>
        private bool StartGuideInternal(GuideData guide, bool forceRestart = false)
        {
            if (IsGuiding)
            {
                if (m_CurrentGuide?.Id == guide.Id && !forceRestart)
                {
                    FuLogger.LogWarning($"[GuideModule] 引导 {guide.Id} 已在运行中");
                    return false;
                }

                FuLogger.LogWarning($"[GuideModule] 中断当前引导 {m_CurrentGuide?.Id}，开始新引导 {guide.Id}");
                InterruptGuide();
            }

            if (IsGuideCompleted(guide.Id) && !forceRestart)
            {
                FuLogger.LogInfo($"[GuideModule] 引导 {guide.Id} 已完成，跳过");
                return false;
            }

            try
            {
                m_CurrentGuide = guide;

                // 构建当前引导下的所有步骤节点
                BuildStepNodes(guide);

                if (guide.StartStepId == 0 || !m_AllStepDict.TryGetValue(guide.StartStepId, out var step))
                {
                    throw new ArgumentException($"起始步骤 ID 无效: {guide.StartStepId}");
                }

                m_CurrentStep = step;

                // 开始执行当前引导的第一个步骤
                ExecuteCurrentStep();

                OnGuideStarted?.Invoke(guide.Id);
                FuLogger.LogInfo($"[GuideModule] 开始引导: {guide.Name} ({guide.Id})");

                return true;
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideModule] 开始引导失败 {guide.Id}: {e.Message}\n{e.StackTrace}");
                ClearGuideData();
                return false;
            }
        }

        /// <summary>
        /// 构建步骤节点
        /// </summary>
        private void BuildStepNodes(GuideData guide)
        {
            // 先回收再 Clear：步骤实例在整条引导生命周期内常驻 m_AllStepDict（唯一回收点是 ClearGuideData），
            // 若此处直接 Clear 而不回收，残留实例会永久滞留在引用池的「使用中」计数里（漏回收）。
            foreach (var (_, step) in m_AllStepDict)
            {
                ReferencePool.Recycle(step);
            }

            m_AllStepDict.Clear();
            m_StepHistoryStack.Clear();

            foreach (var stepInfo in m_StepDataDict.Values)
            {
                if (stepInfo.GuideId != guide.Id) continue;

                var step = CreateStep(stepInfo);
                if (step == null) continue;

                m_AllStepDict.Add(stepInfo.Id, step);
            }

            // 验证步骤链
            ValidateStepChain();
        }

        /// <summary>
        /// 创建步骤
        /// </summary>
        private BaseStep CreateStep(GuideStepData stepInfo)
        {
            return stepInfo.StepType switch
            {
                EStepType.ClickUI => ClickUIStep.Create(stepInfo),
                EStepType.Dialog  => DialogStep.Create(stepInfo),
                EStepType.Wait    => WaitStep.Create(stepInfo),
                EStepType.None    => DefaultStep.Create(stepInfo),
                _                 => DefaultStep.Create(stepInfo)
            };
        }

        /// <summary>
        /// 验证步骤链
        /// </summary>
        private void ValidateStepChain()
        {
            foreach (var step in m_AllStepDict.Values)
            {
                if (step.StepInfo.NextStepId.HasValue && !m_AllStepDict.ContainsKey(step.StepInfo.NextStepId.Value))
                {
                    FuLogger.LogWarning($"[GuideModule] 步骤 {step.StepInfo.Id} 的下一步ID无效: {step.StepInfo.NextStepId}");
                }
            }
        }

        /// <summary>
        /// 执行当前步骤
        /// </summary>
        private void ExecuteCurrentStep()
        {
            var step = m_CurrentStep;
            if (step == null)
            {
                FinishGuide();
                return;
            }

            if (!step.CanExecute())
            {
                FuLogger.LogWarning($"[GuideModule] 步骤 {step.StepInfo.Id} 条件不满足，尝试跳过");
                ForceNextStep();
                return;
            }

            // 局部快照：Execute() 可能同步链式推进（DefaultStep 立即 Complete → JumpToStep / 末步 FinishGuide），
            // 事后读 m_CurrentStep 会拿到末端步骤、甚至已被 ClearGuideData 回收的死对象；
            // 故事件参数、日志、catch 一律用快照，绝不二次解引用 m_CurrentStep（其可能已被置空/回收）。
            var guideId  = CurrentGuideId ?? 0;
            var stepId   = step.StepInfo.Id;
            var stepType = step.StepInfo.StepType;

            try
            {
                // 事件前置：先广播「本步即将执行」，再执行步骤本体。
                // 若放在 Execute() 之后，DefaultStep 的立即 Complete 链会把 m_CurrentStep 推进到末端步骤
                // （末步还会经 FinishGuide 回收本步），监听者将收到错误的步骤、甚至已回池的死对象。
                OnStepExecuting?.Invoke(step);
                OnStepChanged?.Invoke(guideId, stepId);

                // 监听者可能已推动流程（SkipCurrentStep/JumpToStep/InterruptGuide/Complete）：
                // 此时本步已不是当前步，不再执行，避免覆盖新当前步的执行态与计时。
                if (ReferenceEquals(m_CurrentStep, step))
                    step.Execute();

                FuLogger.LogInfo($"[GuideModule] 执行步骤: {stepId} ({stepType})");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideModule] 执行步骤失败 {stepId}: {e.Message}");
                ForceNextStep();
            }
        }

        /// <summary>
        /// 把标脏的引导存档合并落盘（PlayerPrefs.Save 为同步全量落盘，只在每秒更新与模块释放时调用）。
        /// </summary>
        private void FlushGuideData()
        {
            if (!m_GuideDataDirty) return;

            m_GuideDataDirty = false;
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 移动到下一步
        /// </summary>
        private void MoveToNextStep()
        {
            var nextStepId = m_CurrentStep?.StepInfo?.NextStepId;

            if (!nextStepId.HasValue)
            {
                FinishGuide();
                return;
            }

            // 步骤实例在整条引导生命周期内常驻 m_AllStepDict（BuildStepNodes 构建，ClearGuideData 统一回收）。
            // 中途不回收：唯一回收点才能杜绝「同一实例被重复归还」；回退/跳转也因此拿到同一实例、状态不丢失。
            if (!m_AllStepDict.TryGetValue(nextStepId.Value, out var nextStep))
            {
                FinishGuide();
                return;
            }

            m_CurrentStep = nextStep;
            ExecuteCurrentStep();
        }

        /// <summary>
        /// 记录步骤历史(记录 ID；步骤实例在引导结束前常驻字典，回退时按 ID 取回同一实例)
        /// </summary>
        /// <param name="step">步骤实例</param>
        private void PushStepHistory(BaseStep step)
        {
            if (step?.StepInfo == null) return;

            m_StepHistoryStack.Push(step.StepInfo.Id);
        }

        /// <summary>
        /// 完成引导
        /// </summary>
        private void FinishGuide()
        {
            int? finishedGuideId = CurrentGuideId;

            if (finishedGuideId.HasValue)
            {
                MarkGuideAsCompleted(finishedGuideId.Value);
                OnGuideFinished?.Invoke(finishedGuideId.Value);
                FuLogger.LogInfo($"[GuideModule] 引导完成: {finishedGuideId.Value}");
            }

            ClearGuideData();
        }

        /// <summary>
        /// 清理引导数据
        /// </summary>
        private void ClearGuideData()
        {
            // 回收当前引导的所有步骤到引用池中（引导生命周期内步骤的唯一回收点）
            foreach (var (_, step) in m_AllStepDict)
            {
                ReferencePool.Recycle(step);
            }

            m_AllStepDict.Clear();
            m_CurrentStep  = null;
            m_CurrentGuide = null;
            m_StepHistoryStack.Clear();
        }

        #endregion
    }
}
