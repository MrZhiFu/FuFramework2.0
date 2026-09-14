using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using GuideData = Hotfix.Game.Config.Guide;
using AOT.Framework.Core.Log;
using UnityEngine;

namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// 引导管理模块的公共 API。
    /// 功能：
    ///     1. 提供引导的启动、中断接口。
    ///     2. 提供步骤的推进、跳转、回退接口。
    ///     3. 提供引导完成状态的查询与标记。
    ///     4. 提供引导相关事件订阅。
    /// </summary>
    public partial class GuideModule : ModuleBase, ICancelAsync
    {
        #region 单例与状态属性

        /// <summary>
        /// 模块单例
        /// </summary>
        public static GuideModule Instance { get; private set; }

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
        /// 执行引导动作接口。
        /// 赋值时一并捕获其 ICancelAsync 视图，供本模块的 CancelAsync 排水（见 m_GuideActionCancellable）。
        /// </summary>
        public IGuideAction GuideAction
        {
            get => m_GuideAction;
            set
            {
                m_GuideAction           = value;
                m_GuideActionCancellable = value as ICancelAsync;
            }
        }

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

        #region ICancelAsync（框架重启排水）

        /// <summary>
        /// 取消令牌：跟随引导动作执行器的模块级取消范围（两条引导异步链在其中登记在途）。
        /// OnDispose → 引导动作 Dispose 后取消。
        /// </summary>
        public CancellationToken Token => m_GuideActionCancellable?.Token ?? default;

        /// <summary>
        /// 触发取消并等待两条引导异步链（点击UI引导 / 对话引导）清理完毕后返回，可重入、幂等。
        /// 供框架重启（ModuleManager.CancelAllAsync）在 OnDispose 之后等待旧生命周期的在途链结束，
        /// 避免 ReferencePool.ClearAll 在链仍存活时执行。
        /// 未挂接执行器（或执行器未实现 ICancelAsync）时无在途链可排水，直接返回。
        /// </summary>
        public UniTask CancelAsync()
        {
            var cancellable = m_GuideActionCancellable;
            return cancellable?.CancelAsync() ?? UniTask.CompletedTask;
        }

        #endregion

        #region 开始与中断引导

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

        #endregion

        #region 步骤推进

        /// <summary>
        /// 完成当前步骤并进入下一步。
        /// 推进方说明：权威推进方是 BaseStep.Complete 内部的 JumpToStep(NextStepId)（Cancel 当前步骤 + 记入历史 + 执行下一步），
        /// WaitStep/DialogStep/ClickUIStep 都是直接调 Complete() 靠它推进的；故本方法只在「末步」这一分支调 MoveToNextStep() 收尾，
        /// 其余情况不再调，否则一次调用会跨两步、历史栈错位、且被跨过的步骤未经 Cancel（仍挂监听、仍在执行态）。
        /// </summary>
        public void CompleteCurrentStep()
        {
            var completedStep = m_CurrentStep;
            if (completedStep == null) return;

            if (!completedStep.CanComplete())
            {
                FuLogger.LogWarning($"[GuideModule] 步骤 {completedStep.StepInfo.Id} 当前无法完成");
                return;
            }

            try
            {
                // 顺序：先 Complete（推进）再广播完成事件，二者不可调换。
                // 若在推进前广播，此刻该步仍为 Executing、CanComplete() 仍为 true、m_CurrentStep 仍是该步，
                // 监听者在回调里 SkipCurrentStep/JumpToStep/StartGuide 会先推进一次，返回后 Complete 内部的
                // JumpToStep(NextStepId) 分支又会推进一次 → 一步跨两步。
                // 历史不在此重复记录：推进时 JumpToStep 已把当前步骤记入历史，再记一次会让同一 ID 入栈两次。
                completedStep.Complete();

                // 事件针对「被完成的那一步」：completedStep 是推进前捕获的当前步，此刻其 State 已为 Completed，
                // 监听者看到的是「旧步已完成、当前步已切换」的一致状态（不是切换后的新步）。
                OnStepCompleted?.Invoke(completedStep);

                // 末步（无 NextStepId）时 BaseStep.Complete 不会推进，需在此收尾结束引导。
                // 放在广播之后：FinishGuide 会回收步骤实例并把 StepInfo 置空，先回收会把死对象交给监听者。
                // 仅当当前步仍是被完成的那一步（Complete 与监听者都未推动）时才收尾，避免重复推进。
                if (ReferenceEquals(m_CurrentStep, completedStep))
                {
                    MoveToNextStep();
                }
            }
            catch (Exception e)
            {
                // StepInfo 可能已被回收（FinishGuide → ClearGuideData 会置空），故用 ?. 取 ID
                FuLogger.LogError($"[GuideModule] 完成步骤失败 {completedStep.StepInfo?.Id}: {e.Message}");
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
                PushStepHistory(m_CurrentStep);
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

            var previousStepId = m_StepHistoryStack.Pop();
            if (!m_AllStepDict.TryGetValue(previousStepId, out var previousStep))
            {
                FuLogger.LogWarning($"[GuideModule] 历史步骤已不可用: {previousStepId}");
                return;
            }

            m_CurrentStep = previousStep;
            ExecuteCurrentStep();

            FuLogger.LogInfo($"[GuideModule] 返回步骤: {previousStepId}");
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
                PushStepHistory(m_CurrentStep);
            }

            m_CurrentStep = m_AllStepDict[stepId];
            ExecuteCurrentStep();

            FuLogger.LogInfo($"[GuideModule] 跳转到步骤: {stepId}");
            return true;
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

        #endregion

        #region 完成状态管理

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
            // PlayerPrefs.Save() 是同步全量落盘，直接在引导完成/中断路径调用会在帧内阻塞主线程；
            // 故只标脏位，由 OnPerSecondUpdate 合并落盘（至多每秒一次）、OnDispose 兜底落盘。
            m_GuideDataDirty                    = true;
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

            // 删除同样是未落盘的改动，与 MarkGuideAsCompleted 共用同一延迟落盘路径
            m_GuideDataDirty = true;

            FuLogger.LogInfo($"[GuideModule] 重置引导状态: {guideId}");
        }

        #endregion

        #region 步骤与引导查询

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
    }
}
