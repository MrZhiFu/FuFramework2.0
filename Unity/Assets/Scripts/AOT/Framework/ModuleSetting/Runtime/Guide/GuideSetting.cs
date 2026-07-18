using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Runtime.Guide
{
    /// <summary>
    /// 引导配置
    /// </summary>
    [CreateAssetMenu(fileName = "GuideSettings", menuName = "FuFramework/Guide Settings")]
    public class GuideSetting : ScriptableObject
    {
        /// <summary>
        /// 引导列表
        /// </summary>
        [SerializeField] private List<GuideInfo> m_Guides = new();

        /// <summary>
        /// 引导字典，用于快速查找，key为引导ID，value为引导信息
        /// </summary>
        private Dictionary<string, GuideInfo> m_GuideDict;

        /// <summary>
        /// 步骤字典，用于快速查找步骤，key为步骤ID，value为步骤信息
        /// </summary>
        private Dictionary<string, StepInfo> m_StepDict;

        /// <summary>
        /// 是否初始化完成
        /// </summary>
        private bool m_IsInitialized;


        /// <summary>
        /// 获取所有引导
        /// </summary>
        public IReadOnlyList<GuideInfo> AllGuides => m_Guides;

        /// <summary>
        /// 引导数量
        /// </summary>
        public int GuideCount => m_Guides.Count;

        /// <summary>
        /// 总步骤数量
        /// </summary>
        public int TotalStepCount => GetAllSteps().Count;


        #region Get Methods

        /// <summary>
        /// 索引器：通过引导ID获取引导
        /// </summary>
        public GuideInfo this[string guideId]
        {
            get
            {
                InitializeDictionary();
                return m_GuideDict.GetValueOrDefault(guideId);
            }
        }

        /// <summary>
        /// 索引器：通过索引获取引导
        /// </summary>
        public GuideInfo this[int index]
        {
            get
            {
                if (index >= 0 && index < m_Guides.Count) return m_Guides[index];
                return null;
            }
        }

        /// <summary>
        /// 通过ID获取引导
        /// </summary>
        public GuideInfo GetGuide(string guideId)
        {
            InitializeDictionary();
            return m_GuideDict.GetValueOrDefault(guideId);
        }

        /// <summary>
        /// 通过名称获取引导
        /// </summary>
        public GuideInfo GetGuideByName(string guideName)
        {
            InitializeDictionary();
            foreach (var guide in m_Guides)
            {
                if (guide.m_GuideName == guideName)
                    return guide;
            }

            return null;
        }

        /// <summary>
        /// 通过步骤ID获取步骤
        /// </summary>
        public StepInfo GetStep(string stepId)
        {
            InitializeDictionary();
            return m_StepDict.GetValueOrDefault(stepId);
        }

        /// <summary>
        /// 获取指定引导的所有步骤
        /// </summary>
        public List<StepInfo> GetStepsInGuide(string guideId)
        {
            var guide = GetGuide(guideId);
            return guide == null ? new List<StepInfo>() : GetAllStepsInGuide(guide);
        }

        /// <summary>
        /// 获取所有步骤（包括子步骤）
        /// </summary>
        public List<StepInfo> GetAllSteps()
        {
            var allSteps = new List<StepInfo>();
            foreach (var guide in m_Guides)
            {
                allSteps.AddRange(GetAllStepsInGuide(guide));
            }

            return allSteps;
        }

        /// <summary>
        /// 检查是否包含引导
        /// </summary>
        public bool ContainsGuide(string guideId)
        {
            InitializeDictionary();
            return m_GuideDict.ContainsKey(guideId);
        }

        /// <summary>
        /// 检查是否包含步骤
        /// </summary>
        public bool ContainsStep(string stepId)
        {
            InitializeDictionary();
            return m_StepDict.ContainsKey(stepId);
        }

        #endregion

        #region Set Methods

        /// <summary>
        /// 添加引导
        /// </summary>
        public void AddGuide(GuideInfo guideInfo)
        {
            if (guideInfo == null) return;

            InitializeDictionary();

            if (m_GuideDict.ContainsKey(guideInfo.m_GuideId)) return;

            m_Guides.Add(guideInfo);
            m_GuideDict[guideInfo.m_GuideId] = guideInfo;
            UpdateStepDictionary(guideInfo);
        }

        /// <summary>
        /// 创建新的引导
        /// </summary>
        public GuideInfo CreateGuide(string guideName)
        {
            // 确保ID唯一
            var uniqueId = GetUniqueGuideId(guideName);
            var newGuide = new GuideInfo
            {
                m_GuideId   = uniqueId,
                m_GuideName = guideName,
                m_Steps     = new List<StepInfo>()
            };

            AddGuide(newGuide);
            return newGuide;
        }

        /// <summary>
        /// 移除引导
        /// </summary>
        public void RemoveGuide(string guideId)
        {
            InitializeDictionary();

            if (!m_GuideDict.TryGetValue(guideId, out var guide)) return;

            // 从步骤字典中移除所有相关步骤
            foreach (var step in GetAllStepsInGuide(guide))
            {
                m_StepDict.Remove(step.m_StepId);
            }

            m_Guides.Remove(guide);
            m_GuideDict.Remove(guideId);
        }

        /// <summary>
        /// 创建新的步骤
        /// </summary>
        public StepInfo CreateStep(string guideId, string stepName, EStepType stepType = EStepType.None)
        {
            var guide = GetGuide(guideId);
            if (guide == null) return null;

            var stepId = GetUniqueStepId($"Step_{stepName}");
            var newStep = new StepInfo
            {
                m_StepId    = stepId,
                m_EStepType  = stepType,
                m_IsCanJump = false,
                m_WaitTime  = stepType == EStepType.Wait ? 3f : 0f // 等待步骤默认3秒
            };

            return AddStepToGuide(guideId, newStep);
        }

        /// <summary>
        /// 移除步骤
        /// </summary>
        public void RemoveStep(string stepId)
        {
            InitializeDictionary();

            if (!m_StepDict.Remove(stepId))
            {
                Debug.LogError($"步骤 {stepId} 不存在, 无法移除");
                return;
            }

            // 从所有引导的步骤列表中移除该步骤
            foreach (var guide in m_Guides)
            {
                if (guide?.m_Steps != null)
                {
                    RemoveStepFromList(guide.m_Steps, stepId);
                }
            }
        }

        /// <summary>
        /// 从指定步骤列表中移除指定步骤
        /// </summary>
        private void RemoveStepFromList(List<StepInfo> steps, string stepId)
        {
            if (steps == null) return;

            for (var i = steps.Count - 1; i >= 0; i--)
            {
                var step = steps[i];
                if (step == null) continue;

                if (step.m_StepId != stepId) continue;

                steps.RemoveAt(i);
                return;
            }
        }

        /// <summary>
        /// 添加步骤到引导
        /// </summary>
        public StepInfo AddStepToGuide(string guideId, StepInfo stepInfo)
        {
            var guide = GetGuide(guideId);
            if (guide == null || stepInfo == null) return null;

            // 确保步骤ID唯一
            if (m_StepDict.ContainsKey(stepInfo.m_StepId))
            {
                stepInfo.m_StepId = GetUniqueStepId(stepInfo.m_StepId);
            }

            guide.m_Steps.Add(stepInfo);
            m_StepDict[stepInfo.m_StepId] = stepInfo;

            return stepInfo;
        }

        /// <summary>
        /// 清空所有引导
        /// </summary>
        public void ClearAll()
        {
            m_Guides.Clear();
            m_GuideDict?.Clear();
            m_StepDict?.Clear();
            m_IsInitialized = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取唯一的引导ID
        /// </summary>
        private string GetUniqueGuideId(string guideName)
        {
            var guideId = $"Guide_{guideName}";
            var counter = 1;

            while (ContainsGuide(guideId))
            {
                guideId = $"Guide_{guideName}_{counter}";
                counter++;
            }

            return guideId;
        }

        /// <summary>
        /// 获取唯一的步骤ID
        /// </summary>
        private string GetUniqueStepId(string stepId)
        {
            var tempStepId = stepId;
            var counter    = 1;

            while (ContainsStep(tempStepId))
            {
                tempStepId = $"{stepId}_{counter}";
                counter++;
            }

            return tempStepId;
        }

        /// <summary>
        /// 初始化字典
        /// </summary>
        private void InitializeDictionary()
        {
            if (m_IsInitialized           &&
                m_GuideDict       != null &&
                m_StepDict        != null &&
                m_GuideDict.Count == m_Guides.Count)
                return;

            m_GuideDict = new Dictionary<string, GuideInfo>();
            m_StepDict  = new Dictionary<string, StepInfo>();

            foreach (var guide in m_Guides)
            {
                if (guide == null) continue;
                if (string.IsNullOrEmpty(guide.m_GuideId)) continue;
                m_GuideDict.TryAdd(guide.m_GuideId, guide);
                UpdateStepDictionary(guide);
            }

            m_IsInitialized = true;
        }

        /// <summary>
        /// 更新步骤字典
        /// </summary>
        private void UpdateStepDictionary(GuideInfo guide)
        {
            if (guide.m_Steps == null) return;

            foreach (var step in GetAllStepsInGuide(guide))
            {
                if (string.IsNullOrEmpty(step.m_StepId)) continue;
                m_StepDict.TryAdd(step.m_StepId, step);
            }
        }

        /// <summary>
        /// 获取引导中的所有步骤
        /// </summary>
        private List<StepInfo> GetAllStepsInGuide(GuideInfo guide)
        {
            var allSteps = new List<StepInfo>();
            if (guide.m_Steps == null) return allSteps;
            allSteps.AddRange(guide.m_Steps);
            return allSteps;
        }

        /// <summary>
        /// 在编辑器模式下验证数据
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            m_IsInitialized = false;
            InitializeDictionary();
        }

        /// <summary>
        /// 验证引导配置
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            InitializeDictionary();

            foreach (var guide in m_Guides)
            {
                if (string.IsNullOrEmpty(guide.m_GuideId))
                    errors.Add($"引导ID不能为空 (引导: {guide.m_GuideName})");

                if (string.IsNullOrEmpty(guide.m_GuideName))
                    errors.Add($"引导名称不能为空 (ID: {guide.m_GuideId})");

                if (guide.m_Steps == null || guide.m_Steps.Count == 0)
                    errors.Add($"引导 '{guide.m_GuideName}' 没有步骤");
                else
                    ValidateSteps(guide.m_Steps, guide.m_GuideName, errors);

                if (string.IsNullOrEmpty(guide.m_StartStepId))
                    errors.Add($"引导 '{guide.m_GuideName}' 缺少起始步骤");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 验证步骤
        /// </summary>
        private void ValidateSteps(List<StepInfo> steps, string guideName, List<string> errors, string parentPath = "")
        {
            foreach (var step in steps)
            {
                var stepPath = string.IsNullOrEmpty(parentPath)
                    ? step.m_StepId
                    : $"{parentPath}.{step.m_StepId}";

                if (string.IsNullOrEmpty(step.m_StepId))
                    errors.Add($"步骤ID不能为空 (引导: {guideName}, 路径: {stepPath})");
            }
        }

        #endregion
    }
}