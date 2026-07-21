using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Config;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using GuideData = Hotfix.Game.Config.Tables.Guide;
using GuideStepData = Hotfix.Game.Config.Tables.GuideStep;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePools;
using System.Linq;
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
    /// </summary>
    public class GuideModule : ModuleBase
    {
        #region 私有字段

        /// <summary>
        /// 模块单例
        /// </summary>
        public static GuideModule Instance { get; private set; }

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
        /// 步骤历史记录栈
        /// </summary>
        private readonly Stack<BaseStep> m_StepHistoryStack = new();

        /// <summary>
        /// 缓存完成的引导，key为引导ID，Value为是否完成
        /// </summary>
        private readonly Dictionary<int, bool> m_GuideCompletionCacheDict = new();

        #endregion

        #region 事件定义

        /// <summary>
        /// 引导开始事件
        /// </summary>
        public event Action<int> OnGuideStarted;

        /// <summary>
        /// 引导完成事件
        /// </summary>
        public event Action<int> OnGuideFinished;

        /// <summary>
        /// 步骤改变事件
        /// </summary>
        public event Action<int, int> OnStepChanged;

        /// <summary>
        /// 引导中断事件
        /// </summary>
        public event Action<int, bool> OnGuideInterrupted;

        /// <summary>
        /// 步骤开始事件
        /// </summary>
        public event Action<BaseStep> OnStepExecuting;

        /// <summary>
        /// 步骤完成事件
        /// </summary>
        public event Action<BaseStep> OnStepCompleted;

        #endregion

        #region 公开属性

        /// <summary>
        /// 是否正在引导中
        /// </summary>
        public bool IsGuiding => m_CurrentStep != null;

        /// <summary>
        /// 当前引导 ID
        /// </summary>
        public int? CurrentGuideId => m_CurrentGuide?.Id;

        /// <summary>
        /// 当前步骤 ID
        /// </summary>
        public int? CurrentStepId => m_CurrentStep?.StepInfo.Id;

        /// <summary>
        /// 当前引导配置
        /// </summary>
        public GuideData CurrentGuide => m_CurrentGuide;

        /// <summary>
        /// 当前步骤
        /// </summary>
        public BaseStep CurrentStep => m_CurrentStep;

        /// <summary>
        /// 执行引导动作接口
        /// </summary>
        public IGuideAction GuideAction { get; set; }

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
            if (m_CurrentStep is { IsExecuting: true })
                m_CurrentStep.Update(deltaTime);
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            // 中断当前引导
            InterruptGuide();

            // 回收当前引导的所有步骤到引用池中
            foreach (var (_, step) in m_AllStepDict)
            {
                ReferencePool.Release(step);
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

            Instance = null;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 开始引导流程
        /// </summary>
        /// <param name="guideId">引导 ID</param>
        /// <param name="forceRestart">是否强制重新开始</param>
        /// <returns>是否成功开始引导</returns>
        public bool StartGuide(int guideId, bool forceRestart = false)
        {
            if (!m_GuideDict.TryGetValue(guideId, out var guide))
            {
                FuLogger.LogError($"[GuideModule] 找不到引导: {guideId}");
                return false;
            }

            return StartGuideInternal(guide, forceRestart);
        }

        /// <summary>
        /// 开始第一个引导
        /// </summary>
        public bool StartFirstGuide(bool forceRestart = false)
        {
            GuideData firstGuide = null;
            foreach (var guide in m_GuideDict.Values)
            {
                firstGuide = guide;
                break;
            }

            if (firstGuide == null)
            {
                FuLogger.LogError("[GuideModule] 没有可用的引导");
                return false;
            }

            return StartGuideInternal(firstGuide, forceRestart);
        }

        /// <summary>
        /// 完成当前步骤并进入下一步
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (m_CurrentStep == null) return;

            if (!m_CurrentStep.CanComplete())
            {
                FuLogger.LogWarning($"[GuideModule] 步骤 {m_CurrentStep.StepInfo.Id} 当前无法完成");
                return;
            }

            try
            {
                m_CurrentStep.Complete();
                m_StepHistoryStack.Push(m_CurrentStep);
                OnStepCompleted?.Invoke(m_CurrentStep);

                MoveToNextStep();
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideModule] 完成步骤失败 {m_CurrentStep.StepInfo.Id}: {e.Message}");
                ForceNextStep();
            }
        }

        /// <summary>
        /// 跳过当前步骤
        /// </summary>
        public void SkipCurrentStep()
        {
            if (m_CurrentStep == null) return;

            if (m_CurrentStep.StepInfo.CanJump)
            {
                FuLogger.LogInfo($"[GuideModule] 跳过可选步骤: {m_CurrentStep.StepInfo.Id}");
                m_CurrentStep.Cancel();
                m_StepHistoryStack.Push(m_CurrentStep);
                MoveToNextStep();
            }
            else
            {
                FuLogger.LogWarning($"[GuideModule] 步骤 {m_CurrentStep.StepInfo.Id} 不可跳过");
            }
        }

        /// <summary>
        /// 返回上一步
        /// </summary>
        public void GoToPreviousStep()
        {
            if (m_StepHistoryStack.Count == 0)
            {
                FuLogger.LogWarning("[GuideModule] 没有历史步骤可返回");
                return;
            }

            m_CurrentStep?.Cancel();

            var previousStep = m_StepHistoryStack.Pop();
            m_CurrentStep = previousStep;
            ExecuteCurrentStep();

            FuLogger.LogInfo($"[GuideModule] 返回步骤: {previousStep.StepInfo.Id}");
        }

        /// <summary>
        /// 跳转到指定步骤
        /// </summary>
        /// <param name="stepId">步骤ID</param>
        /// <returns>是否跳转成功</returns>
        public bool JumpToStep(int stepId)
        {
            if (!m_AllStepDict.ContainsKey(stepId))
            {
                FuLogger.LogError($"[GuideModule] 步骤ID不存在: {stepId}");
                return false;
            }

            if (m_CurrentStep != null)
            {
                m_CurrentStep.Cancel();
                m_StepHistoryStack.Push(m_CurrentStep);
            }

            m_CurrentStep = m_AllStepDict[stepId];
            ExecuteCurrentStep();

            FuLogger.LogInfo($"[GuideModule] 跳转到步骤: {stepId}");
            return true;
        }

        /// <summary>
        /// 中断引导
        /// </summary>
        /// <param name="markAsCompleted">是否标记为已完成</param>
        public void InterruptGuide(bool markAsCompleted = false)
        {
            if (m_CurrentStep == null) return;

            var guideId = CurrentGuideId;

            m_CurrentStep.Cancel();

            if (markAsCompleted && guideId.HasValue)
            {
                MarkGuideAsCompleted(guideId.Value);
            }

            OnGuideInterrupted?.Invoke(guideId ?? 0, markAsCompleted);
            FuLogger.LogInfo($"[GuideModule] 引导中断: {guideId}, 标记完成: {markAsCompleted}");

            ClearGuideData();
        }

        /// <summary>
        /// 强制进入下一步(跳过条件检查)
        /// </summary>
        public void ForceNextStep()
        {
            if (m_CurrentStep == null) return;

            var nextStepId = m_CurrentStep.StepInfo.NextStepId;
            m_CurrentStep.Cancel();

            if (nextStepId.HasValue && m_AllStepDict.TryGetValue(nextStepId.Value, out var nextStep))
            {
                m_CurrentStep = nextStep;
                ExecuteCurrentStep();
            }
            else
            {
                FinishGuide();
            }
        }

        /// <summary>
        /// 检查引导是否已完成
        /// </summary>
        /// <param name="guideId">引导ID</param>
        /// <returns>是否已完成</returns>
        public bool IsGuideCompleted(int guideId)
        {
            if (m_GuideCompletionCacheDict.TryGetValue(guideId, out var completed))
            {
                return completed;
            }

            completed = PlayerPrefs.GetInt($"Guide_Completed_{guideId}", 0) == 1;

            m_GuideCompletionCacheDict[guideId] = completed;
            return completed;
        }

        /// <summary>
        /// 标记引导为已完成
        /// </summary>
        /// <param name="guideId">引导ID</param>
        public void MarkGuideAsCompleted(int guideId)
        {
            PlayerPrefs.SetInt($"Guide_Completed_{guideId}", 1);
            PlayerPrefs.Save();
            m_GuideCompletionCacheDict[guideId] = true;

            FuLogger.LogInfo($"[GuideModule] 标记引导为已完成: {guideId}");
        }

        /// <summary>
        /// 重置引导状态
        /// </summary>
        /// <param name="guideId">引导ID</param>
        public void ResetGuide(int guideId)
        {
            PlayerPrefs.DeleteKey($"Guide_Completed_{guideId}");
            m_GuideCompletionCacheDict.Remove(guideId);

            FuLogger.LogInfo($"[GuideModule] 重置引导状态: {guideId}");
        }

        /// <summary>
        /// 获取步骤实例
        /// </summary>
        /// <param name="stepId">步骤ID</param>
        /// <returns>步骤实例</returns>
        public BaseStep GetStep(int stepId) => m_AllStepDict.GetValueOrDefault(stepId);

        /// <summary>
        /// 获取所有步骤
        /// </summary>
        /// <returns>步骤字典</returns>
        public Dictionary<int, BaseStep> GetAllSteps() => new(m_AllStepDict);

        /// <summary>
        /// 获取当前引导信息
        /// </summary>
        public GuideData GetCurrentGuideInfo() => m_CurrentGuide;

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
            m_AllStepDict.Clear();
            m_StepHistoryStack.Clear();

            var guideSteps = m_StepDataDict.Values
                .Where(s => s.GuideId == guide.Id)
                .ToList();

            foreach (var stepInfo in guideSteps)
            {
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
            if (m_CurrentStep == null)
            {
                FinishGuide();
                return;
            }

            if (!m_CurrentStep.CanExecute())
            {
                FuLogger.LogWarning($"[GuideModule] 步骤 {m_CurrentStep.StepInfo.Id} 条件不满足，尝试跳过");
                ForceNextStep();
                return;
            }

            try
            {
                m_CurrentStep.Execute();
                OnStepExecuting?.Invoke(m_CurrentStep);
                OnStepChanged?.Invoke(CurrentGuideId ?? 0, CurrentStepId ?? 0);

                FuLogger.LogInfo($"[GuideModule] 执行步骤: {m_CurrentStep.StepInfo.Id} ({m_CurrentStep.StepInfo.StepType})");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideModule] 执行步骤失败 {m_CurrentStep.StepInfo.Id}: {e.Message}");
                ForceNextStep();
            }
        }

        /// <summary>
        /// 移动到下一步
        /// </summary>
        private void MoveToNextStep()
        {
            var nextStepId = m_CurrentStep?.StepInfo?.NextStepId;

            if (nextStepId.HasValue && m_AllStepDict.TryGetValue(nextStepId.Value, out var nextStep))
            {
                ReferencePool.Release(m_CurrentStep); // 回收当前步骤到引用池中
                m_CurrentStep = nextStep;
                ExecuteCurrentStep();
            }
            else
            {
                FinishGuide();
            }
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
            // 回收当前引导的所有步骤到引用池中
            foreach (var (_, step) in m_AllStepDict)
            {
                ReferencePool.Release(step);
            }

            m_AllStepDict.Clear();
            m_CurrentStep  = null;
            m_CurrentGuide = null;
            m_StepHistoryStack.Clear();
        }

        #endregion
    }
}
