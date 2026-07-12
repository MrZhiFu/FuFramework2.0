#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Editor
{
    public partial class RedDotSettingEditor
    {
        /// <summary>
        /// 检查key格式是否符合要求
        /// </summary>
        private bool ValidateKeyFormat(RedDotNodeData node, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 空key检查
            if (string.IsNullOrEmpty(node.m_Key))
            {
                errorMessage = "Key不能为空！";
                return false;
            }

            // 基本字符检查
            if (!IsValidKeyCharacters(node.m_Key))
            {
                errorMessage = "Key只能包含字母和点号！";
                return false;
            }

            // 根节点检查
            if (IsRootNode(node))
            {
                // 根节点不能包含点号
                if (node.m_Key.Contains("."))
                {
                    errorMessage = "根节点Key不能包含点号(.)！";
                    return false;
                }
            }
            else
            {
                // 子节点检查：必须包含点号，格式为 父节点.子节点
                if (!node.m_Key.Contains("."))
                {
                    errorMessage = "子节点Key格式必须为：父节点.子节点";
                    return false;
                }

                // 获取父节点
                var parentNode = FindParentNode(node);
                if (parentNode != null)
                {
                    // 检查子节点key是否以父节点key开头
                    string expectedPrefix = parentNode.m_Key + ".";
                    if (!node.m_Key.StartsWith(expectedPrefix))
                    {
                        errorMessage = $"子节点Key格式错误，应该以父节点Key开头。当前父节点: {parentNode.m_Key}，期望格式: {parentNode.m_Key}.子节点名";
                        return false;
                    }

                    // 检查子节点名称部分是否为空
                    string childName = node.m_Key.Substring(expectedPrefix.Length);
                    if (string.IsNullOrEmpty(childName))
                    {
                        errorMessage = "子节点名称不能为空！";
                        return false;
                    }
                }
            }

            // 检查点号位置
            if (node.m_Key.StartsWith(".") || node.m_Key.EndsWith("."))
            {
                errorMessage = "Key不能以点号开头或结尾！";
                return false;
            }

            // 检查连续点号
            if (node.m_Key.Contains(".."))
            {
                errorMessage = "Key不能包含连续的点号！";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否为根节点
        /// </summary>
        private bool IsRootNode(RedDotNodeData node)
        {
            var setting = target as RedDotSetting;
            return setting != null && setting.m_RootNodes.Contains(node);
        }

        /// <summary>
        /// 检查key字符是否有效(只能包含字母和点号)
        /// </summary>
        private bool IsValidKeyCharacters(string key)
        {
            return !string.IsNullOrEmpty(key) && key.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '.');
        }

        /// <summary>
        /// 刷新重复key和格式错误缓存
        /// </summary>
        private void RefreshValidationCache()
        {
            m_DuplicateKeyPaths.Clear();
            m_InvalidFormatNodes.Clear();

            var setting = target as RedDotSetting;
            if (setting == null) return;

            var keyToPaths   = new Dictionary<string, List<(string path, RedDotNodeData node)>>();
            var invalidNodes = new List<(RedDotNodeData node, string path, string error)>();

            // 递归收集所有key和路径，并检查格式
            void CollectAndValidateNodes(RedDotNodeData node, string currentPath)
            {
                if (node == null) return;

                var nodePath = string.IsNullOrEmpty(currentPath) ? node.m_Key : $"{currentPath}/{node.m_Key}";

                // 检查key格式
                if (!ValidateKeyFormat(node, out var errorMessage))
                {
                    invalidNodes.Add((node, nodePath, errorMessage));
                }

                // 收集重复key信息
                if (keyToPaths.ContainsKey(node.m_Key))
                    keyToPaths[node.m_Key].Add((nodePath, node));
                else
                    keyToPaths[node.m_Key] = new List<(string path, RedDotNodeData node)> { (nodePath, node) };

                foreach (var child in node.m_Children)
                {
                    CollectAndValidateNodes(child, nodePath);
                }
            }

            // 遍历所有根节点开始收集
            foreach (var rootNode in setting.m_RootNodes)
            {
                CollectAndValidateNodes(rootNode, "");
            }

            // 找出重复的key
            m_DuplicateKeyPaths = keyToPaths
                                  .Where(kvp => kvp.Value.Count > 1)
                                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 存储格式错误的节点
            m_InvalidFormatNodes = invalidNodes;
        }

        /// <summary>
        /// 强制刷新验证检查
        /// </summary>
        private void ForceRefreshValidationCheck()
        {
            RefreshValidationCache();
            Repaint();
        }

        /// <summary>
        /// 检查key是否重复
        /// </summary>
        private bool IsKeyDuplicate(string key, RedDotNodeData excludeNode = null)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var setting = target as RedDotSetting;
            if (setting == null) return false;

            // 递归检查key是否重复
            bool CheckNode(RedDotNodeData node)
            {
                if (node       == excludeNode) return false;
                if (node.m_Key == key) return true;

                foreach (var child in node.m_Children)
                {
                    if (CheckNode(child)) return true;
                }

                return false;
            }

            foreach (var rootNode in setting.m_RootNodes)
            {
                if (CheckNode(rootNode)) return true;
            }

            return false;
        }
    }
}
#endif