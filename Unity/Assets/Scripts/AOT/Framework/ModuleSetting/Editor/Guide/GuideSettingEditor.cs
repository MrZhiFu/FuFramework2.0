#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.ModuleSetting.Runtime.Guide;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor.Guide
{
    /// <summary>
    /// 引导配置文件Inspector
    /// </summary>
    [CustomEditor(typeof(GuideSetting))]
    public class GuideSettingEditor : UnityEditor.Editor
    {
        /// 引导列表属性
        private SerializedProperty m_GuidesProperty;

        /// 引导折叠状态数组
        private bool[] m_GuideFoldouts;

        /// 引导步骤折叠状态字典，key：引导ID，value：该引导下的步骤折叠状态数组
        private readonly Dictionary<string, bool> m_StepFoldouts = new();

        private bool    m_ShowTools;                  // 是否显示工具区域
        private string  m_NewGuideName = "New Guide"; // 新引导名称
        private Vector2 m_ScrollPosition;             // 滚动位置


        private const int GUIDE_INDENT_LEVEL = 0;  // 引导的缩进级别
        private const int STEP_INDENT_LEVEL  = 1;  // 步骤的缩进级别
        private const int INDENT_WIDTH       = 20; // 每个缩进级别的宽度

        /// <summary>
        /// 编辑器启用时调用
        /// </summary>
        private void OnEnable()
        {
            m_GuidesProperty = serializedObject.FindProperty("m_Guides");
            UpdateFoldoutState();
        }

        /// <summary>
        /// 绘制检视面板GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var guideSetting = target as GuideSetting;
            if (!guideSetting) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("引导配置管理", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"引导数量: {guideSetting.GuideCount} | 总步骤数量: {guideSetting.TotalStepCount}", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            // 显示引导列表
            DisplayGuidesList(guideSetting);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(20);

            // 工具区域
            DisplayToolsArea(guideSetting);

            // 修改应用
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 显示引导列表
        /// </summary>
        private void DisplayGuidesList(GuideSetting setting)
        {
            if (m_GuidesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("没有引导配置，点击下方按钮添加", MessageType.Info);
                return;
            }

            for (var i = 0; i < m_GuidesProperty.arraySize; i++)
            {
                var guideProperty = m_GuidesProperty.GetArrayElementAtIndex(i);
                if (guideProperty != null)
                {
                    DisplayGuide(i, guideProperty, setting);
                }

                EditorGUILayout.Space(10);
            }
        }

        /// <summary>
        /// 显示单个引导
        /// </summary>
        private void DisplayGuide(int index, SerializedProperty guideProperty, GuideSetting setting)
        {
            var guideIdProperty     = guideProperty.FindPropertyRelative("m_GuideId");
            var guideNameProperty   = guideProperty.FindPropertyRelative("m_GuideName");
            var startStepIdProperty = guideProperty.FindPropertyRelative("m_StartStepId");
            var stepsProperty       = guideProperty.FindPropertyRelative("m_Steps");

            // 确保折叠状态数组足够大
            if (m_GuideFoldouts == null || m_GuideFoldouts.Length <= index)
                UpdateFoldoutState();

            if (m_GuideFoldouts is null) return;

            EditorGUILayout.BeginVertical("box");

            // 标题行
            EditorGUILayout.BeginHorizontal();
            var foldoutLabel = $"{guideNameProperty.stringValue} (ID: {guideIdProperty.stringValue})";
            m_GuideFoldouts[index] = EditorGUILayout.Foldout(m_GuideFoldouts[index], foldoutLabel, true);

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                RemoveGuide(setting, index);
                return;
            }

            EditorGUILayout.EndHorizontal();

            // 折叠内容
            if (m_GuideFoldouts[index])
            {
                EditorGUILayout.Space(5);

                // 基本信息
                EditorGUILayout.PropertyField(guideIdProperty,   new GUIContent("引导ID"));
                EditorGUILayout.PropertyField(guideNameProperty, new GUIContent("引导名称"));

                // 起始步骤选择
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("起始步骤");
                if (setting[index] != null && setting[index].m_Steps.Count > 0)
                {
                    var stepIds   = new List<string> { "(无)" };
                    var stepNames = new List<string> { "(无)" };

                    foreach (var step in GetAllSteps(setting[index].m_Steps))
                    {
                        stepIds.Add(step.m_StepId);
                        stepNames.Add($"{step.m_StepId} ({step.m_EStepType})");
                    }

                    var currentIndex                   = stepIds.IndexOf(startStepIdProperty.stringValue);
                    if (currentIndex < 0) currentIndex = 0;

                    var newIndex = EditorGUILayout.Popup(currentIndex, stepNames.ToArray());
                    if (newIndex != currentIndex)
                    {
                        startStepIdProperty.stringValue = newIndex == 0 ? "" : stepIds[newIndex];
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("无可用步骤");
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                // 步骤列表
                EditorGUILayout.LabelField("步骤列表", EditorStyles.boldLabel);
                DisplayStepsList(stepsProperty, setting, guideIdProperty.stringValue);

                EditorGUILayout.Space(5);

                // 添加步骤按钮
                if (GUILayout.Button("+ 添加步骤"))
                {
                    AddNewStep(setting, guideIdProperty.stringValue);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 显示步骤列表
        /// </summary>
        private void DisplayStepsList(SerializedProperty stepsProperty, GuideSetting setting, string guideId)
        {
            if (stepsProperty.arraySize == 0)
            {
                // 添加缩进
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(STEP_INDENT_LEVEL * INDENT_WIDTH);
                EditorGUILayout.HelpBox("没有步骤，点击按钮添加", MessageType.Info);
                EditorGUILayout.EndHorizontal();
                return;
            }

            for (var i = 0; i < stepsProperty.arraySize; i++)
            {
                var stepProperty = stepsProperty.GetArrayElementAtIndex(i);

                // 为步骤添加缩进容器
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(STEP_INDENT_LEVEL * INDENT_WIDTH);
                EditorGUILayout.BeginVertical();

                DisplayStep(stepProperty, setting, guideId);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
            }
        }

        /// <summary>
        /// 显示单个步骤
        /// </summary>
        private void DisplayStep(SerializedProperty stepProperty, GuideSetting setting, string guideId)
        {
            var stepIdProperty       = stepProperty.FindPropertyRelative("m_StepId");
            var stepTypeProperty     = stepProperty.FindPropertyRelative("m_EStepType");
            var nextStepIdProperty   = stepProperty.FindPropertyRelative("m_NextStepId");
            var canJumpProperty      = stepProperty.FindPropertyRelative("m_IsCanJump");
            var targetWindowProperty = stepProperty.FindPropertyRelative("m_TargetWindow");
            var targetUIProperty     = stepProperty.FindPropertyRelative("m_TargetUI");
            var dialogProperty       = stepProperty.FindPropertyRelative("m_DialogContent");
            var waitTimeProperty     = stepProperty.FindPropertyRelative("m_WaitTime");

            var stepKey = stepIdProperty.stringValue;
            m_StepFoldouts.TryAdd(stepKey, true);

            EditorGUILayout.BeginVertical("box");

            // 步骤标题行
            EditorGUILayout.BeginHorizontal();

            // 步骤标题
            var stepLabel = $"{stepIdProperty.stringValue} [{stepTypeProperty.enumDisplayNames[stepTypeProperty.enumValueIndex]}]";
            m_StepFoldouts[stepKey] = EditorGUILayout.Foldout(m_StepFoldouts[stepKey], stepLabel, true);

            // 删除按钮 - 向右对齐
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                RemoveStep(setting, stepIdProperty.stringValue);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            // 步骤详情
            if (m_StepFoldouts[stepKey])
            {
                EditorGUILayout.Space(5);

                // 为步骤详情添加额外缩进
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(INDENT_WIDTH);
                EditorGUILayout.BeginVertical();

                EditorGUILayout.PropertyField(stepIdProperty,   new GUIContent("步骤ID"));
                EditorGUILayout.PropertyField(stepTypeProperty, new GUIContent("步骤类型"));
                EditorGUILayout.PropertyField(canJumpProperty,  new GUIContent("是否可跳过"));

                // 根据步骤类型显示不同的字段
                var stepType = (EStepType)stepTypeProperty.enumValueIndex;
                switch (stepType)
                {
                    case EStepType.Dialog:
                        EditorGUILayout.PropertyField(dialogProperty, new GUIContent("对话内容"));
                        break;
                    case EStepType.ClickUI:
                        EditorGUILayout.PropertyField(targetWindowProperty, new GUIContent("目标窗口"));
                        EditorGUILayout.PropertyField(targetUIProperty,     new GUIContent("目标UI"));
                        break;
                    case EStepType.Wait:
                        EditorGUILayout.PropertyField(waitTimeProperty, new GUIContent("等待时间(秒)"));
                        break;
                }

                // 下一个步骤选择
                var guide = setting.GetGuide(guideId);
                if (guide != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("下一个步骤");

                    var allSteps  = GetAllSteps(guide.m_Steps);
                    var stepIds   = new List<string> { "(无)", "(结束)" };
                    var stepNames = new List<string> { "(无)", "(结束)" };

                    foreach (var step in allSteps.Where(s => s.m_StepId != stepIdProperty.stringValue))
                    {
                        stepIds.Add(step.m_StepId);
                        stepNames.Add($"{step.m_StepId} ({step.m_EStepType})");
                    }

                    var currentIndex = stepIds.IndexOf(nextStepIdProperty.stringValue);
                    if (currentIndex < 0)
                        currentIndex = 0;

                    var newIndex = EditorGUILayout.Popup(currentIndex, stepNames.ToArray());
                    if (newIndex != currentIndex)
                    {
                        nextStepIdProperty.stringValue = newIndex <= 1 ? "" : stepIds[newIndex];
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 显示工具区域
        /// </summary>
        private void DisplayToolsArea(GuideSetting setting)
        {
            m_ShowTools = EditorGUILayout.Foldout(m_ShowTools, "工具", true);
            if (m_ShowTools)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("引导管理", EditorStyles.boldLabel);

                // 添加新引导
                m_NewGuideName = EditorGUILayout.TextField("引导名称", m_NewGuideName);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ 添加引导", GUILayout.Height(30)))
                {
                    AddNewGuide(setting, m_NewGuideName);
                    m_NewGuideName = "New Guide";
                }

                if (GUILayout.Button("添加示例引导", GUILayout.Height(30)))
                {
                    AddExampleGuides(setting);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                // 批量操作
                EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("全部展开"))
                {
                    ExpandAll();
                }

                if (GUILayout.Button("全部折叠"))
                {
                    CollapseAll();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 清空按钮
                if (GUILayout.Button("清空所有引导", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有引导吗？", "确定", "取消"))
                    {
                        ClearAllGuides(setting);
                    }
                }

                // 验证按钮
                if (GUILayout.Button("验证配置"))
                {
                    ValidateSetting(setting);
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 添加新引导
        /// </summary>
        private void AddNewGuide(GuideSetting setting, string guideName)
        {
            if (string.IsNullOrEmpty(guideName))
            {
                EditorUtility.DisplayDialog("错误", "引导名称不能为空", "确定");
                return;
            }

            var newGuide = setting.CreateGuide(guideName);
            if (newGuide == null) return;

            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log($"已创建引导: {guideName}");
        }

        /// <summary>
        /// 添加示例引导
        /// </summary>
        private void AddExampleGuides(GuideSetting setting)
        {
            // 添加示例引导
            const string exampleName = "新手引导";

            if (setting.ContainsGuide($"Guide_{exampleName}")) return;

            var guide = setting.CreateGuide(exampleName);
            if (guide != null)
            {
                // 为每个引导添加一个示例步骤
                var step = setting.CreateStep(guide.m_GuideId, "示例步骤", EStepType.ClickUI);
                if (step != null)
                {
                    step.m_TargetWindow  = "ExampleWindow";
                    step.m_TargetUI      = "ExampleUI";
                    step.m_DialogContent = $"这是{exampleName}的示例步骤";
                }

                guide.m_StartStepId = step?.m_StepId;
            }

            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log("已添加示例引导");
        }

        /// <summary>
        /// 添加新步骤
        /// </summary>
        private void AddNewStep(GuideSetting setting, string guideId)
        {
            var guide = setting.GetGuide(guideId);
            if (guide == null) return;

            var stepName = $"步骤{guide.m_Steps.Count + 1}";
            var step     = setting.CreateStep(guideId, stepName, EStepType.ClickUI);

            if (step != null)
            {
                step.m_TargetWindow = "TargetWindow";
                step.m_TargetUI     = "TargetUI";
                step.m_IsCanJump    = true;

                if (guide.m_Steps.Count == 1)
                {
                    guide.m_StartStepId = step.m_StepId;
                }
            }

            serializedObject.Update();
            EditorUtility.SetDirty(setting);
        }

        /// <summary>
        /// 移除引导
        /// </summary>
        private void RemoveGuide(GuideSetting setting, int index)
        {
            var guide = setting[index];
            if (guide == null) return;

            if (!EditorUtility.DisplayDialog("确认删除", $"确定要删除引导 '{guide.m_GuideName}' 及其所有步骤吗？", "删除", "取消"))
                return;

            setting.RemoveGuide(guide.m_GuideId);
            serializedObject.Update();
            UpdateFoldoutState();
            EditorUtility.SetDirty(setting);

            Debug.Log($"已删除引导: {guide.m_GuideName}");
        }

        /// <summary>
        /// 移除步骤
        /// </summary>
        private void RemoveStep(GuideSetting setting, string stepId)
        {
            var step = setting.GetStep(stepId);
            if (step == null) return;

            if (!EditorUtility.DisplayDialog("确认删除", $"确定要删除步骤 '{stepId}' 吗？", "删除", "取消"))
                return;

            setting.RemoveStep(stepId);
            serializedObject.Update();
            m_StepFoldouts.Remove(stepId);
            EditorUtility.SetDirty(setting);

            Debug.Log($"已删除步骤: {stepId}");
        }

        /// <summary>
        /// 清空所有引导
        /// </summary>
        private void ClearAllGuides(GuideSetting setting)
        {
            setting.ClearAll();
            serializedObject.Update();
            UpdateFoldoutState();
            m_StepFoldouts.Clear();
            EditorUtility.SetDirty(setting);

            Debug.Log("已清空所有引导");
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private void ValidateSetting(GuideSetting setting)
        {
            if (setting.Validate(out var errors))
            {
                EditorUtility.DisplayDialog("验证通过", "引导配置验证通过！", "确定");
            }
            else
            {
                var errorMessage = $"验证失败，发现 {errors.Count} 个错误：\n\n";
                foreach (var error in errors.Take(10)) // 最多显示10个错误
                {
                    errorMessage += $"• {error}\n";
                }

                if (errors.Count > 10)
                {
                    errorMessage += $"\n... 还有 {errors.Count - 10} 个错误";
                }

                EditorUtility.DisplayDialog("验证失败", errorMessage, "确定");
            }
        }

        /// <summary>
        /// 展开所有
        /// </summary>
        private void ExpandAll()
        {
            if (m_GuideFoldouts == null) return;

            for (var i = 0; i < m_GuideFoldouts.Length; i++)
            {
                m_GuideFoldouts[i] = true;
            }
        }

        /// <summary>
        /// 折叠所有
        /// </summary>
        private void CollapseAll()
        {
            if (m_GuideFoldouts == null) return;

            for (var i = 0; i < m_GuideFoldouts.Length; i++)
            {
                m_GuideFoldouts[i] = false;
            }
        }

        /// <summary>
        /// 更新折叠状态数组
        /// </summary>
        private void UpdateFoldoutState()
        {
            m_GuideFoldouts = new bool[m_GuidesProperty.arraySize];
            for (var i = 0; i < m_GuideFoldouts.Length; i++)
            {
                m_GuideFoldouts[i] = true; // 默认展开
            }
        }

        /// <summary>
        /// 获取所有步骤（递归）
        /// </summary>
        private List<StepInfo> GetAllSteps(List<StepInfo> steps)
        {
            var allSteps = new List<StepInfo>();
            if (steps == null) return allSteps;
            allSteps.AddRange(steps);
            return allSteps;
        }
    }
}
#endif