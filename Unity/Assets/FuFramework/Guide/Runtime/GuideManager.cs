using System;
using System.Linq;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;
using UnityEngine;

namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// 引导管理器
    /// </summary>
    public class GuideManager : FuModule
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;

        #region 私有字段

        /// 引导配置SO
        private GuideSetting m_Setting;

        /// 当前引导
        private GuideInfo m_CurrentGuide;

        /// 当前引导中的当前步骤
        private BaseStep m_CurrentStep;

        /// 当前引导中的所有步骤，key为步骤Id，Value为步骤对象
        private readonly Dictionary<string, BaseStep> m_AllStepDict = new();

        /// 步骤历史记录栈
        private readonly Stack<BaseStep> m_StepHistoryStack = new();

        /// 缓存完成的引导，key为引导ID，Value为是否完成
        private readonly Dictionary<string, bool> m_GuideCompletionCacheDict = new();

        #endregion

        #region 事件定义

        /// <summary>
        /// 引导开始事件
        /// </summary>
        public event Action<string> OnGuideStarted;

        /// <summary>
        /// 引导完成事件
        /// </summary>
        public event Action<string> OnGuideFinished;

        /// <summary>
        /// 步骤改变事件
        /// </summary>
        public event Action<string, string> OnStepChanged;

        /// <summary>
        /// 引导中断事件
        /// </summary>
        public event Action<string, bool> OnGuideInterrupted;

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
        /// 当前引导ID
        /// </summary>
        public string CurrentGuideId => m_CurrentGuide?.m_GuideId;

        /// <summary>
        /// 当前步骤ID
        /// </summary>
        public string CurrentStepId => m_CurrentStep?.StepInfo.m_StepId;

        /// <summary>
        /// 当前引导配置
        /// </summary>
        public GuideInfo CurrentGuide => m_CurrentGuide;

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
        protected override void OnInit()
        {
            m_GuideCompletionCacheDict.Clear();
            m_Setting = ModuleSetting.Runtime.ModuleSetting.Instance.GuideSetting;
            if (m_Setting == null)
            {
                FuLogger.LogError("[GuideManager] 配置文件不存在.");
                return;
            }

            FuLogger.LogInfo("[GuideManager] 引导管理器初始化完成");
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型</param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            // 中断当前引导
            InterruptGuide();

            // 回收当前引导的所有步骤到引用池中
            foreach (var (_, step) in m_AllStepDict)
            {
                ReferencePool.Runtime.ReferencePool.Release(step);
            }

            m_AllStepDict.Clear();
            m_StepHistoryStack.Clear();
            m_GuideCompletionCacheDict.Clear();
            m_CurrentGuide = null;

            // 清理事件订阅
            OnGuideStarted     = null;
            OnGuideFinished    = null;
            OnStepChanged      = null;
            OnGuideInterrupted = null;
            OnStepExecuting    = null;
            OnStepCompleted    = null;
        }

        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (m_CurrentStep is { IsExecuting: true })
                m_CurrentStep.Update(elapseSeconds);
        }

        #endregion

        #region 公开方法
        
        /// <summary>
        /// 开始引导流程(通过引导ID)
        /// </summary>
        /// <param name="guideId">引导ID</param>
        /// <param name="forceRestart">是否强制重新开始</param>
        /// <returns>是否成功开始引导</returns>
        public bool StartGuideById(string guideId, bool forceRestart = false)
        {
            try
            {
                return StartGuide(guideId, forceRestart);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideManager] 加载引导配置失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 开始引导流程(通过引导名称)
        /// </summary>
        /// <param name="guideName">引导名称</param>
        /// <param name="forceRestart">是否强制重新开始</param>
        /// <returns>是否成功开始引导</returns>
        public bool StartGuideByName(string guideName, bool forceRestart = false)
        {
            try
            {
                var guide = m_Setting.GetGuideByName(guideName);
                if (guide == null)
                {
                    FuLogger.LogError($"[GuideManager] 找不到引导: {guideName}");
                    return false;
                }

                return StartGuide(guide, forceRestart);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideManager] 加载引导配置失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 开始第一个引导
        /// </summary>
        public bool StartFirstGuide(bool forceRestart = false)
        {
            try
            {
                var firstGuide = m_Setting.AllGuides.FirstOrDefault();
                if (firstGuide == null)
                {
                    FuLogger.LogError("[GuideManager] 没有可用的引导");
                    return false;
                }

                return StartGuide(firstGuide, forceRestart);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideManager] 开始引导失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 开始引导流程
        /// </summary>
        /// <param name="guideId">要开始的引导ID</param>
        /// <param name="forceRestart">是否强制重新开始</param>
        /// <returns>是否成功开始引导</returns>
        public bool StartGuide(string guideId, bool forceRestart = false)
        {
            var guideInfo = m_Setting.GetGuide(guideId);
            if (guideInfo == null)
            {
                FuLogger.LogError($"[GuideManager] 找不到引导: {guideId}");
                return false;
            }

            return StartGuide(guideInfo, forceRestart);
        }

        /// <summary>
        /// 开始引导流程(通过GuideInfo)
        /// </summary>
        /// <param name="guideInfo">引导信息</param>
        /// <param name="forceRestart">是否强制重新开始</param>
        /// <returns>是否成功开始引导</returns>
        public bool StartGuide(GuideInfo guideInfo, bool forceRestart = false)
        {
            if (guideInfo == null)
            {
                FuLogger.LogError("[GuideManager] 引导信息为空");
                return false;
            }

            if (IsGuiding)
            {
                if (CurrentGuideId == guideInfo.m_GuideId && !forceRestart)
                {
                    FuLogger.LogWarning($"[GuideManager] 引导 {guideInfo.m_GuideId} 已在运行中");
                    return false;
                }

                FuLogger.LogWarning($"[GuideManager] 中断当前引导 {CurrentGuideId}，开始新引导 {guideInfo.m_GuideId}");
                InterruptGuide();
            }

            if (IsGuideCompleted(guideInfo.m_GuideId) && !forceRestart)
            {
                FuLogger.LogInfo($"[GuideManager] 引导 {guideInfo.m_GuideId} 已完成，跳过");
                return false;
            }

            try
            {
                m_CurrentGuide = guideInfo;

                // 构建当前引导下的所有步骤节点
                BuildStepNodes(guideInfo);

                if (string.IsNullOrEmpty(guideInfo.m_StartStepId))
                {
                    throw new ArgumentException("引导配置缺少起始步骤ID");
                }

                if (!m_AllStepDict.TryGetValue(guideInfo.m_StartStepId, out var step))
                {
                    throw new ArgumentException($"起始步骤ID不存在: {guideInfo.m_StartStepId}");
                }

                m_CurrentStep = step;

                // 开始执行当前引导的第一个步骤
                ExecuteCurrentStep();

                OnGuideStarted?.Invoke(guideInfo.m_GuideId);
                FuLogger.LogInfo($"[GuideManager] 开始引导: {guideInfo.m_GuideName} ({guideInfo.m_GuideId})");

                return true;
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideManager] 开始引导失败 {guideInfo.m_GuideId}: {e.Message}\n{e.StackTrace}");
                ClearGuideData();
                return false;
            }
        }

        /// <summary>
        /// 完成当前步骤并进入下一步
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (m_CurrentStep == null) return;

            if (!m_CurrentStep.CanComplete())
            {
                FuLogger.LogWarning($"[GuideManager] 步骤 {m_CurrentStep.StepInfo.m_StepId} 当前无法完成");
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
                FuLogger.LogError($"[GuideManager] 完成步骤失败 {m_CurrentStep.StepInfo.m_StepId}: {e.Message}");
                ForceNextStep();
            }
        }

        /// <summary>
        /// 跳过当前步骤
        /// </summary>
        public void SkipCurrentStep()
        {
            if (m_CurrentStep == null) return;

            if (m_CurrentStep.StepInfo.m_IsCanJump)
            {
                FuLogger.LogInfo($"[GuideManager] 跳过可选步骤: {m_CurrentStep.StepInfo.m_StepId}");
                m_CurrentStep.Cancel();
                m_StepHistoryStack.Push(m_CurrentStep);
                MoveToNextStep();
            }
            else
            {
                FuLogger.LogWarning($"[GuideManager] 步骤 {m_CurrentStep.StepInfo.m_StepId} 不可跳过");
            }
        }

        /// <summary>
        /// 返回上一步
        /// </summary>
        public void GoToPreviousStep()
        {
            if (m_StepHistoryStack.Count == 0)
            {
                FuLogger.LogWarning("[GuideManager] 没有历史步骤可返回");
                return;
            }

            m_CurrentStep?.Cancel();

            var previousStep = m_StepHistoryStack.Pop();
            m_CurrentStep = previousStep;
            ExecuteCurrentStep();

            FuLogger.LogInfo($"[GuideManager] 返回步骤: {previousStep.StepInfo.m_StepId}");
        }

        /// <summary>
        /// 跳转到指定步骤
        /// </summary>
        /// <param name="stepId">步骤ID</param>
        /// <returns>是否跳转成功</returns>
        public bool JumpToStep(string stepId)
        {
            if (!m_AllStepDict.ContainsKey(stepId))
            {
                FuLogger.LogError($"[GuideManager] 步骤ID不存在: {stepId}");
                return false;
            }

            if (m_CurrentStep != null)
            {
                m_CurrentStep.Cancel();
                m_StepHistoryStack.Push(m_CurrentStep);
            }

            m_CurrentStep = m_AllStepDict[stepId];
            ExecuteCurrentStep();

            FuLogger.LogInfo($"[GuideManager] 跳转到步骤: {stepId}");
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

            if (markAsCompleted && !string.IsNullOrEmpty(guideId))
            {
                MarkGuideAsCompleted(guideId);
            }

            OnGuideInterrupted?.Invoke(guideId, markAsCompleted);
            FuLogger.LogInfo($"[GuideManager] 引导中断: {guideId}, 标记完成: {markAsCompleted}");

            ClearGuideData();
        }

        /// <summary>
        /// 强制进入下一步(跳过条件检查)
        /// </summary>
        public void ForceNextStep()
        {
            if (m_CurrentStep == null) return;

            var nextStepId = m_CurrentStep.StepInfo.m_NextStepId;
            m_CurrentStep.Cancel();

            if (!string.IsNullOrEmpty(nextStepId) && m_AllStepDict.TryGetValue(nextStepId, out var nextStep))
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
        public bool IsGuideCompleted(string guideId)
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
        public void MarkGuideAsCompleted(string guideId)
        {
            PlayerPrefs.SetInt($"Guide_Completed_{guideId}", 1);
            PlayerPrefs.Save();
            m_GuideCompletionCacheDict[guideId] = true;

            FuLogger.LogInfo($"[GuideManager] 标记引导为已完成: {guideId}");
        }

        /// <summary>
        /// 重置引导状态
        /// </summary>
        /// <param name="guideId">引导ID</param>
        public void ResetGuide(string guideId)
        {
            PlayerPrefs.DeleteKey($"Guide_Completed_{guideId}");
            m_GuideCompletionCacheDict.Remove(guideId);

            FuLogger.LogInfo($"[GuideManager] 重置引导状态: {guideId}");
        }

        /// <summary>
        /// 获取步骤实例
        /// </summary>
        /// <param name="stepId">步骤ID</param>
        /// <returns>步骤实例</returns>
        public BaseStep GetStep(string stepId) => m_AllStepDict.GetValueOrDefault(stepId);

        /// <summary>
        /// 获取所有步骤
        /// </summary>
        /// <returns>步骤字典</returns>
        public Dictionary<string, BaseStep> GetAllSteps() => new(m_AllStepDict);

        /// <summary>
        /// 获取当前引导信息
        /// </summary>
        public GuideInfo GetCurrentGuideInfo() => m_CurrentGuide;

        #endregion

        #region 私有方法

        /// <summary>
        /// 构建步骤节点
        /// </summary>
        private void BuildStepNodes(GuideInfo info)
        {
            m_AllStepDict.Clear();
            m_StepHistoryStack.Clear();

            foreach (var stepInfo in info.m_Steps)
            {
                var step = CreateStep(stepInfo);
                if (step == null) continue;

                m_AllStepDict.Add(step.StepInfo.m_StepId, step);
            }

            // 验证步骤链
            ValidateStepChain();
        }

        /// <summary>
        /// 创建步骤
        /// </summary>
        /// <param name="stepInfo"></param>
        /// <returns></returns>
        private BaseStep CreateStep(StepInfo stepInfo)
        {
            return stepInfo.m_StepType switch
            {
                StepType.ClickUI => ClickUIStep.Create(stepInfo),
                StepType.Dialog  => DialogStep.Create(stepInfo),
                StepType.Wait    => WaitStep.Create(stepInfo),
                StepType.None    => DefaultStep.Create(stepInfo),
                _                => DefaultStep.Create(stepInfo)
            };
        }

        /// <summary>
        /// 验证步骤链
        /// </summary>
        private void ValidateStepChain()
        {
            foreach (var step in m_AllStepDict.Values)
            {
                if (!string.IsNullOrEmpty(step.StepInfo.m_NextStepId) && !m_AllStepDict.ContainsKey(step.StepInfo.m_StepId))
                {
                    FuLogger.LogWarning($"[GuideManager] 步骤 {step.StepInfo.m_StepId} 的下一步ID无效: {step.StepInfo.m_NextStepId}");
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
                FuLogger.LogWarning($"[GuideManager] 步骤 {m_CurrentStep.StepInfo.m_StepId} 条件不满足，尝试跳过");
                ForceNextStep();
                return;
            }

            try
            {
                m_CurrentStep.Execute();
                OnStepExecuting?.Invoke(m_CurrentStep);
                OnStepChanged?.Invoke(CurrentGuideId, m_CurrentStep.StepInfo.m_StepId);

                FuLogger.LogInfo($"[GuideManager] 执行步骤: {m_CurrentStep.StepInfo.m_StepId} ({m_CurrentStep.StepInfo.m_StepType})");
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideManager] 执行步骤失败 {m_CurrentStep.StepInfo.m_StepId}: {e.Message}");
                ForceNextStep();
            }
        }

        /// <summary>
        /// 移动到下一步
        /// </summary>
        private void MoveToNextStep()
        {
            var nextStepId = m_CurrentStep?.StepInfo?.m_NextStepId;

            if (!string.IsNullOrEmpty(nextStepId) && m_AllStepDict.TryGetValue(nextStepId, out var nextStep))
            {
                ReferencePool.Runtime.ReferencePool.Release(m_CurrentStep); // 回收当前步骤到引用池中
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
            string finishedGuideId = CurrentGuideId;

            if (!string.IsNullOrEmpty(finishedGuideId))
            {
                MarkGuideAsCompleted(finishedGuideId);
                OnGuideFinished?.Invoke(finishedGuideId);
                FuLogger.LogInfo($"[GuideManager] 引导完成: {finishedGuideId}");
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
                ReferencePool.Runtime.ReferencePool.Release(step);
            }

            m_AllStepDict.Clear();
            m_CurrentStep  = null;
            m_CurrentGuide = null;
            m_StepHistoryStack.Clear();
        }

        #endregion
    }
}