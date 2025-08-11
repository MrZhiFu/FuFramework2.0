using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuFramework.Core.Runtime;
using UnityEditor;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 防裁剪代码生成窗口
    /// </summary>
    public sealed class CroppingWindow : EditorWindow
    {
        /// 类型选择下拉框
        private string[] _dropdownOptions = { "Empty" };

        /// 忽略的类型
        private readonly string[] _ignoredTypes =
        {
            "UnityEngine".ToLower(),
            "UnityEditor".ToLower(),
            "Mono".ToLower(),
            "System".ToLower(),
            "dnlib".ToLower(),
            "Unity.Hotfix".ToLower(),
            "Unity.Baselib".ToLower(),
            ".Editor".ToLower(),
            "JetBrains".ToLower(),
            "NUnit".ToLower()
        };

        /// 选择的类型下标
        private int _selectedDropdownIndex = 0;

        /// 搜索文本框
        private string _searchText = string.Empty;


        [MenuItem("GameFrameX/Cropping(防止裁剪代码生成)", false, 2001)]
        public static void ShowWindow()
        {
            var window = GetWindow<CroppingWindow>("Cropping");
            window.minSize   = new Vector2(800, 600);
            window.maxSize   = window.minSize;
            window.maximized = false;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label("查询类型:", EditorStyles.label, GUILayout.Width(100)); // 设置宽度以确保一致性
                _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarTextField, GUILayout.Width(600));
                GUILayout.FlexibleSpace(); // 使中间部分可以自动伸缩

                if (GUILayout.Button("Search(查询)", EditorStyles.toolbarButton))
                {
                    if (string.IsNullOrWhiteSpace(_searchText))
                    {
                        ShowNotification(new GUIContent { text = "搜索内容不能为空" });
                    }
                    else
                    {
                        // 获取所有类型，并过滤掉忽略的类型
                        var types  = Utility.Assembly.GetTypes();
                        var result = new List<string>();
                        foreach (var type in types)
                        {
                            if (type.FullName == null) continue;
                            var fullName = type.FullName.ToLower();

                            var isIgnored = false;
                            foreach (var ignoredType in _ignoredTypes)
                            {
                                if (!fullName.Contains(ignoredType.ToLower())) continue;
                                isIgnored = true;
                                break;
                            }

                            if (isIgnored) continue;

                            if (fullName.Contains(_searchText.ToLower()))
                                result.Add(type.FullName);
                        }

                        _dropdownOptions = result.ToArray();
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5); // 使中间部分可以自动伸缩
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("类型选择:", EditorStyles.label, GUILayout.Width(100));
                var newDropdownIndex = EditorGUILayout.Popup(_selectedDropdownIndex, _dropdownOptions, EditorStyles.toolbarDropDown, GUILayout.Width(600));
                if (!newDropdownIndex.Equals(_selectedDropdownIndex))
                {
                    _selectedDropdownIndex = newDropdownIndex;
                }

                GUILayout.FlexibleSpace(); // 使中间部分可以自动伸缩
                if (GUILayout.Button("Generate(生成)", EditorStyles.toolbarButton))
                {
                    Generate(_dropdownOptions[_selectedDropdownIndex]);
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.TextArea(_generatedText, GUILayout.ExpandHeight(true));
        }

        /// <summary>
        /// 生成的代码文本
        /// </summary>
        private string _generatedText = string.Empty;

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="targetTypeName"></param>
        private void Generate(string targetTypeName)
        {
            _generatedText = string.Empty;
            var targetType = Utility.Assembly.GetType(targetTypeName);
            if (targetType == null) return;

            var types = targetType.Assembly.GetTypes();
            types = types.OrderBy(m => m.FullName).ToArray();
            var sb = new StringBuilder();
            foreach (var type in types)
            {
                if (type.FullName == null) continue;
                if (type.IsNestedPrivate) continue;

                if (type.FullName.Contains("PrivateImplementationDetails")) continue;
                sb.AppendLine(" _ = typeof(" + type.FullName.Replace("+", ".").Replace("`1", "<>").Replace("`2", "<,>") + ");");
            }

            _generatedText = sb.ToString();
            ShowNotification(new GUIContent { text = "请将代码复制到CroppingHelper.cs 中" });
        }
    }
}