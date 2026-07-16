using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UtilityAOT = FuFramework.Core.Runtime.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 防裁剪代码生成窗口。
    /// 功能：
    ///     1. 用于生成防裁剪代码，防止代码被裁剪。
    /// </summary>
    public sealed class CroppingWindow : EditorWindow
    {
        /// 类型选择下拉框
        private string[] m_dropdownOptions = { "Empty" };

        /// 忽略的类型
        private readonly string[] m_ignoredTypes =
        {
            "UnityEngine".ToLower(),
            "UnityEditor".ToLower(),
            "Mono".ToLower(),
            "System".ToLower(),
            "dnlib".ToLower(),
            "Hotfix".ToLower(),
            "Unity.Baselib".ToLower(),
            ".Editor".ToLower(),
            "JetBrains".ToLower(),
            "NUnit".ToLower()
        };

        /// 选择的类型下标
        private int m_selectedDropdownIndex;

        /// 搜索文本框
        private string m_searchText = string.Empty;

        /// 生成的代码文本
        private string m_generatedText = string.Empty;

        /// 搜索缓存
        private readonly Dictionary<string, string[]> m_searchCache = new();

        /// 生成选项
        private bool m_includeNestedTypes = true;
        private bool m_includeGenericTypes = true;
        private bool m_includePrivateTypes = false;

        /// 文件生成选项
        private string m_customFileName = "";
        private bool m_autoCreateFolder = true;

        /// 滚动位置
        private Vector2 m_scrollPosition = Vector2.zero;

        /// 当前选择的类型信息
        private string m_currentTypeName = "";
        private string m_currentTypeAssetPath = "";
        private string m_targetNamespace = "";


        [MenuItem("FuFramework/代码防裁剪工具", false, 1200)]
        public static void ShowWindow()
        {
            var window = GetWindow<CroppingWindow>("Cropping");
            window.minSize = new Vector2(800, 600);
            window.maxSize = new Vector2(1200, 800);
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawOptions();
            DrawFileOptions();
            DrawContentArea();
        }

        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label("查询类型:", EditorStyles.label, GUILayout.Width(100));
                m_searchText = EditorGUILayout.TextField(m_searchText, EditorStyles.toolbarTextField, GUILayout.Width(400));
                
                if (GUILayout.Button("查询", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    SearchTypes();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("清除缓存", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    m_searchCache.Clear();
                    ShowNotification(new GUIContent { text = "缓存已清除" });
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制选项区域
        /// </summary>
        private void DrawOptions()
        {
            GUILayout.Space(5);

            // 类型选择行
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("类型选择:", EditorStyles.label, GUILayout.Width(100));
                var newDropdownIndex = EditorGUILayout.Popup(m_selectedDropdownIndex, m_dropdownOptions, EditorStyles.toolbarDropDown, GUILayout.Width(400));
                if (!newDropdownIndex.Equals(m_selectedDropdownIndex))
                {
                    m_selectedDropdownIndex = newDropdownIndex;
                    UpdateTypeInfo();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("生成", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    GenerateCode();
                }
            }
            GUILayout.EndHorizontal();

            // 选项行
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("生成选项:", EditorStyles.label, GUILayout.Width(100));
                
                m_includeNestedTypes = EditorGUILayout.ToggleLeft("包含嵌套类型", m_includeNestedTypes, GUILayout.Width(120));
                m_includeGenericTypes = EditorGUILayout.ToggleLeft("包含泛型类型", m_includeGenericTypes, GUILayout.Width(120));
                m_includePrivateTypes = EditorGUILayout.ToggleLeft("包含私有类型", m_includePrivateTypes, GUILayout.Width(120));
                
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        /// <summary>
        /// 绘制文件选项区域
        /// </summary>
        private void DrawFileOptions()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("文件生成选项", EditorStyles.boldLabel);
                
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("目标类型:", EditorStyles.label, GUILayout.Width(100));
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(m_currentTypeName) ? "未选择" : m_currentTypeName, EditorStyles.textField);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(m_targetNamespace))
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("命名空间:", EditorStyles.label, GUILayout.Width(100));
                        EditorGUILayout.LabelField(m_targetNamespace, EditorStyles.textField);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("文件名称:", EditorStyles.label, GUILayout.Width(100));
                    m_customFileName = EditorGUILayout.TextField(m_customFileName, GUILayout.Width(300));
                    if (GUILayout.Button("使用默认", GUILayout.Width(60)))
                    {
                        m_customFileName = GetDefaultFileName();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("保存路径:", EditorStyles.label, GUILayout.Width(100));
                    EditorGUILayout.LabelField(GetSaveDirectory(), EditorStyles.textField);
                    if (GUILayout.Button("浏览", GUILayout.Width(60)))
                    {
                        BrowseSaveDirectory();
                    }
                    if (GUILayout.Button("重置路径", GUILayout.Width(60)))
                    {
                        ResetToTypePath();
                    }
                }
                GUILayout.EndHorizontal();

                m_autoCreateFolder = EditorGUILayout.ToggleLeft("自动创建文件夹（如不存在）", m_autoCreateFolder);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("生成并保存文件", GUILayout.Width(120)))
                    {
                        GenerateAndSaveFile();
                    }
                    if (GUILayout.Button("打开所在文件夹", GUILayout.Width(120)))
                    {
                        OpenSaveDirectory();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
        }

        /// <summary>
        /// 绘制内容区域
        /// </summary>
        private void DrawContentArea()
        {
            GUILayout.Label("生成的防裁剪代码:", EditorStyles.boldLabel);
            
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));
            {
                var textAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    fontSize = 12
                };
                
                m_generatedText = EditorGUILayout.TextArea(m_generatedText, textAreaStyle, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();

            // 底部统计信息
            if (!string.IsNullOrEmpty(m_generatedText))
            {
                var lineCount = m_generatedText.Split('\n').Length;
                GUILayout.Label($"生成完成，共 {lineCount} 行代码", EditorStyles.helpBox);
            }
        }

        /// <summary>
        /// 更新类型信息
        /// </summary>
        private void UpdateTypeInfo()
        {
            if (m_dropdownOptions.Length == 0 || m_selectedDropdownIndex >= m_dropdownOptions.Length)
                return;

            var targetTypeName = m_dropdownOptions[m_selectedDropdownIndex];
            if (string.IsNullOrEmpty(targetTypeName) || targetTypeName == "Empty")
                return;

            try
            {
                var targetType = UtilityAOT.Assembly.GetType(targetTypeName);
                if (targetType != null)
                {
                    m_currentTypeName = targetTypeName;
                    m_targetNamespace = targetType.Namespace ?? "";
                    m_currentTypeAssetPath = FindTypeAssetPath(targetType);
                    m_customFileName = GetDefaultFileName();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"获取类型信息失败: {ex.Message}");
                m_currentTypeName = "未知类型";
                m_targetNamespace = "";
                m_currentTypeAssetPath = "Assets";
            }
        }

        /// <summary>
        /// 查找类型在Assets中的路径
        /// </summary>
        private string FindTypeAssetPath(System.Type targetType)
        {
            try
            {
                var typeName = targetType.Name;
                var namespaceName = targetType.Namespace ?? "";

                // 方法1: 通过类型名称精确查找脚本文件
                var allScriptPaths = AssetDatabase.FindAssets($"t:Script {typeName}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => path.EndsWith(".cs"))
                    .ToArray();

                foreach (var scriptPath in allScriptPaths)
                {
                    // 读取脚本文件内容，检查是否包含该类型
                    var scriptContent = File.ReadAllText(scriptPath);
                    if (scriptContent.Contains($"class {typeName}") || 
                        scriptContent.Contains($"struct {typeName}") ||
                        scriptContent.Contains($"interface {typeName}"))
                    {
                        // 如果命名空间也匹配，则更精确
                        if (string.IsNullOrEmpty(namespaceName) || scriptContent.Contains($"namespace {namespaceName}"))
                        {
                            return Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
                        }
                    }
                }

                // 方法2: 通过命名空间猜测路径
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    var namespacePath = namespaceName.Replace('.', '/');
                    var possiblePaths = new[]
                    {
                        $"Assets/{namespacePath}",
                        $"Assets/Scripts/{namespacePath}",
                        $"Assets/{namespacePath}/Scripts",
                        namespacePath
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (AssetDatabase.IsValidFolder(path))
                        {
                            return path;
                        }
                    }

                    // 如果文件夹不存在，但命名空间有效，创建对应的文件夹结构
                    var assetsNamespacePath = $"Assets/{namespacePath}";
                    return assetsNamespacePath;
                }

                // 方法3: 查找程序集对应的asmdef文件
                var assemblyName = targetType.Assembly.GetName().Name;
                var asmdefPaths = AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();

                if (asmdefPaths.Length > 0)
                {
                    return Path.GetDirectoryName(asmdefPaths[0])?.Replace("\\", "/");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"查找类型路径时出错: {ex.Message}");
            }

            return "Assets";
        }

        /// <summary>
        /// 重置到类型路径
        /// </summary>
        private void ResetToTypePath()
        {
            UpdateTypeInfo();
            ShowNotification(new GUIContent { text = "路径已重置到目标类型所在文件夹" });
        }

        /// <summary>
        /// 获取默认文件名
        /// </summary>
        private string GetDefaultFileName()
        {
            return "CroppingClassDefine.cs";
        }

        /// <summary>
        /// 获取保存目录
        /// </summary>
        private string GetSaveDirectory()
        {
            // 优先使用目标类型所在文件夹路径
            if (!string.IsNullOrEmpty(m_currentTypeAssetPath))
            {
                // 检查路径是否存在，如果不存在但启用了自动创建，则返回该路径
                if (m_autoCreateFolder || AssetDatabase.IsValidFolder(m_currentTypeAssetPath))
                {
                    return m_currentTypeAssetPath;
                }
            }

            // 备用方案：使用Assets根目录
            return "Assets";
        }

        /// <summary>
        /// 浏览保存目录
        /// </summary>
        private void BrowseSaveDirectory()
        {
            var defaultPath = GetSaveDirectory();
            var fullDefaultPath = defaultPath;
            
            if (defaultPath.StartsWith("Assets"))
            {
                fullDefaultPath = Path.Combine(Application.dataPath, defaultPath.Replace("Assets/", "").Replace("Assets", ""));
            }

            var selectedPath = EditorUtility.SaveFolderPanel("选择保存目录", fullDefaultPath, "");
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 转换为Assets相对路径
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    m_currentTypeAssetPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    // 如果不在Assets目录内，提示用户
                    if (EditorUtility.DisplayDialog("路径警告", 
                        "选择的路径不在Assets目录内，这可能导致Unity无法识别文件。是否继续？", "继续", "取消"))
                    {
                        m_currentTypeAssetPath = selectedPath;
                    }
                }
            }
        }

        /// <summary>
        /// 打开保存目录
        /// </summary>
        private void OpenSaveDirectory()
        {
            var directory = GetSaveDirectory();
            var fullPath = directory;
            
            // 如果是Assets相对路径，转换为完整路径
            if (directory.StartsWith("Assets"))
            {
                fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", "").Replace("Assets", ""));
            }
            
            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else if (m_autoCreateFolder)
            {
                // 如果启用了自动创建，显示将创建的路径
                ShowNotification(new GUIContent { text = "目录不存在，保存时将自动创建" });
            }
            else
            {
                ShowNotification(new GUIContent { text = "目录不存在" });
            }
        }

        /// <summary>
        /// 搜索类型
        /// </summary>
        private void SearchTypes()
        {
            if (string.IsNullOrWhiteSpace(m_searchText))
            {
                ShowNotification(new GUIContent { text = "搜索内容不能为空" });
                return;
            }

            try
            {
                // 检查缓存
                var cacheKey = m_searchText.ToLower();
                if (m_searchCache.TryGetValue(cacheKey, out var options))
                {
                    m_dropdownOptions = options;
                    ShowNotification(new GUIContent { text = $"从缓存加载 {m_dropdownOptions.Length} 个类型" });
                    return;
                }

                // 获取所有类型，并过滤掉忽略的类型
                var types = UtilityAOT.Assembly.GetTypes();
                var result = new List<string>();
                
                foreach (var type in types)
                {
                    if (type.FullName == null) continue;
                    var fullName = type.FullName.ToLower();

                    // 过滤掉忽略的类型
                    var isIgnored = m_ignoredTypes.Any(ignoredType => fullName.Contains(ignoredType.ToLower()));
                    if (isIgnored) continue;

                    if (fullName.Contains(m_searchText.ToLower()))
                        result.Add(type.FullName);
                }

                m_dropdownOptions = result.OrderBy(x => x).ToArray();
                m_searchCache[cacheKey] = m_dropdownOptions;
                
                ShowNotification(new GUIContent { text = $"找到 {m_dropdownOptions.Length} 个匹配类型" });
                
                // 更新类型信息
                if (m_dropdownOptions.Length > 0)
                {
                    m_selectedDropdownIndex = 0;
                    UpdateTypeInfo();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"搜索类型时出错: {ex.Message}");
                ShowNotification(new GUIContent { text = "搜索失败，请查看控制台日志" });
            }
        }

        /// <summary>
        /// 生成防裁剪代码
        /// </summary>
        private void GenerateCode()
        {
            if (m_dropdownOptions.Length == 0 || m_selectedDropdownIndex >= m_dropdownOptions.Length)
            {
                ShowNotification(new GUIContent { text = "没有可用的类型数据" });
                return;
            }

            var targetTypeName = m_dropdownOptions[m_selectedDropdownIndex];
            
            if (string.IsNullOrEmpty(targetTypeName) || targetTypeName == "Empty")
            {
                ShowNotification(new GUIContent { text = "请选择有效的类型" });
                return;
            }

            try
            {
                var targetType = UtilityAOT.Assembly.GetType(targetTypeName);
                if (targetType == null)
                {
                    ShowNotification(new GUIContent { text = $"找不到类型: {targetTypeName}" });
                    return;
                }

                m_generatedText = GenerateCroppingCode(targetType);
                ShowNotification(new GUIContent { text = "代码生成完成" });
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成防裁剪代码时出错: {ex.Message}\n{ex.StackTrace}");
                ShowNotification(new GUIContent { text = "生成失败，请查看控制台日志" });
                m_generatedText = $"// 生成错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 生成并保存文件
        /// </summary>
        private void GenerateAndSaveFile()
        {
            if (m_dropdownOptions.Length == 0 || m_selectedDropdownIndex >= m_dropdownOptions.Length)
            {
                ShowNotification(new GUIContent { text = "没有可用的类型数据" });
                return;
            }

            var targetTypeName = m_dropdownOptions[m_selectedDropdownIndex];
            
            if (string.IsNullOrEmpty(targetTypeName) || targetTypeName == "Empty")
            {
                ShowNotification(new GUIContent { text = "请选择有效的类型" });
                return;
            }

            try
            {
                var targetType = UtilityAOT.Assembly.GetType(targetTypeName);
                if (targetType == null)
                {
                    ShowNotification(new GUIContent { text = $"找不到类型: {targetTypeName}" });
                    return;
                }

                // 更新类型信息（确保路径是最新的）
                UpdateTypeInfo();

                // 生成代码
                var code = GenerateCroppingCode(targetType);
                m_generatedText = code;

                // 确定文件名
                var fileName = string.IsNullOrEmpty(m_customFileName) ? GetDefaultFileName() : m_customFileName;
                if (!fileName.EndsWith(".cs"))
                    fileName += ".cs";

                // 确定保存路径 - 使用目标类型所在文件夹
                var saveDirectory = GetSaveDirectory();
                var fullPath = Path.Combine(saveDirectory, fileName).Replace("\\", "/");

                // 确保目录存在
                if (m_autoCreateFolder)
                {
                    var fullDirectory = saveDirectory;
                    if (saveDirectory.StartsWith("Assets"))
                    {
                        fullDirectory = Path.Combine(Application.dataPath, saveDirectory.Replace("Assets/", "").Replace("Assets", ""));
                    }
                    
                    if (!Directory.Exists(fullDirectory))
                    {
                        Directory.CreateDirectory(fullDirectory);
                        Debug.Log($"创建目录: {fullDirectory}");
                    }
                }

                // 保存文件
                File.WriteAllText(fullPath, code, Encoding.UTF8);

                // 刷新Unity资产数据库
                AssetDatabase.Refresh();

                ShowNotification(new GUIContent { text = $"文件已保存到目标类型所在文件夹: {fullPath}" });
                Debug.Log($"防裁剪代码文件已保存: {fullPath}");

                // 选中新创建的文件
                var relativePath = fullPath;
                if (fullPath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);
                }
                
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存文件时出错: {ex.Message}\n{ex.StackTrace}");
                ShowNotification(new GUIContent { text = "保存失败，请查看控制台日志" });
            }
        }

        /// <summary>
        /// 生成防裁剪代码内容
        /// </summary>
        private string GenerateCroppingCode(System.Type targetType)
        {
            // 获取类型所在的程序集的所有类型，并根据全名排序
            var types = targetType.Assembly.GetTypes();
            types = types.OrderBy(m => m.FullName).ToArray();
            
            var sb = new StringBuilder();
            var assemblyName = targetType.Assembly.GetName().Name;
            var namespaceName = targetType.Namespace ?? "";
            
            sb.AppendLine("// ===================================================");
            sb.AppendLine("// 防裁剪代码 - 自动生成");
            sb.AppendLine("// 程序集: " + assemblyName);
            sb.AppendLine("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("// 保存位置: " + GetSaveDirectory());
            sb.AppendLine("// ===================================================");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            // 添加命名空间（与目标类型保持一致）
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("// ReSharper disable once CheckNamespace");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 防裁剪类定义");
            sb.AppendLine("    /// 防止 IL2CPP 代码裁剪时移除重要类型");
            sb.AppendLine("    /// 自动生成于目标类型所在文件夹");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class CroppingClassDefine");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 防止代码裁剪的方法");
            sb.AppendLine("        /// 在场景加载前执行，确保所有需要的类型都被保留");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
            sb.AppendLine("        private static void PreventCodeStripping()");
            sb.AppendLine("        {");

            var validTypeCount = 0;
            
            // 遍历所有类型，写入类型防裁剪代码
            foreach (var type in types)
            {
                if (!ShouldIncludeType(type)) continue;

                var typeName = FormatTypeName(type);
                if (string.IsNullOrEmpty(typeName)) continue;

                // 根据是否在命名空间内调整缩进
                var indent = string.IsNullOrEmpty(namespaceName) ? "        " : "            ";
                sb.AppendLine($"{indent}_ = typeof({typeName});");
                validTypeCount++;
            }

            if (validTypeCount == 0)
            {
                var indent = string.IsNullOrEmpty(namespaceName) ? "        " : "            ";
                sb.AppendLine($"{indent}// 没有找到需要防裁剪的类型");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            
            // 关闭命名空间
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 判断是否应该包含该类型
        /// </summary>
        private bool ShouldIncludeType(System.Type type)
        {
            if (type.FullName == null) return false;

            // 过滤掉忽略的类型
            var fullName = type.FullName.ToLower();
            if (m_ignoredTypes.Any(ignoredType => fullName.Contains(ignoredType.ToLower()))) return false;

            // 根据选项过滤
            if (type.IsNestedPrivate && !m_includePrivateTypes) return false;
            if (type.IsNested && !m_includeNestedTypes) return false;
            if (type.IsGenericType && !m_includeGenericTypes) return false;

            if (type.FullName.Contains("PrivateImplementationDetails")) return false;
            return true;
        }

        /// <summary>
        /// 格式化类型名称
        /// </summary>
        private string FormatTypeName(System.Type type)
        {
            var typeName = type.Name
                .Replace("+", ".")  // 嵌套类分隔符
                .Replace("`1", "<>") // 泛型类型
                .Replace("`2", "<,>")
                .Replace("`3", "<,,>")
                .Replace("`4", "<,,,>");

            return typeName;
        }

        private void OnInspectorUpdate()
        {
            // 定期重绘，确保通知信息正常显示
            Repaint();
        }
    }
}