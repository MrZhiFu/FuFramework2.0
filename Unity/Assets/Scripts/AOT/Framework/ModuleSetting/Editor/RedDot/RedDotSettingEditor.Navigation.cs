#if UNITY_EDITOR
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.ModuleSetting.Runtime.RedDot;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Editor.RedDot
{
    public partial class RedDotSettingEditor
    {
        /// <summary>
        /// 定位并展开节点
        /// </summary>
        private void LocateAndExpandNode(RedDotNodeData targetNode)
        {
            if (targetNode == null) return;

            // 递归展开父节点
            void ExpandParentNodes(RedDotNodeData node)
            {
                var parent = FindParentNode(node);
                if (parent != null)
                {
                    // 确保父节点在字典中存在并设置为展开状态
                    m_NodeExpanded[parent] = true;
                    ExpandParentNodes(parent);
                }
            }

            // 展开所有父节点
            ExpandParentNodes(targetNode);

            // 确保目标节点在字典中存在
            m_NodeExpanded[targetNode] = true;

            // 标记需要重绘
            Repaint();
        }

        /// <summary>
        /// 查找节点的父节点
        /// </summary>
        private RedDotNodeData FindParentNode(RedDotNodeData targetNode)
        {
            var setting = target as RedDotSetting;
            if (setting == null) return null;

            // 递归查找父节点
            RedDotNodeData FindParentRecursive(RedDotNodeData currentNode, RedDotNodeData childNode)
            {
                if (currentNode.m_Children.Contains(childNode))
                    return currentNode;

                foreach (var child in currentNode.m_Children)
                {
                    var result = FindParentRecursive(child, childNode);
                    if (result != null)
                        return result;
                }

                return null;
            }

            foreach (var rootNode in setting.m_RootNodes)
            {
                if (rootNode == targetNode)
                    return null; // 根节点没有父节点

                var parent = FindParentRecursive(rootNode, targetNode);
                if (parent != null)
                    return parent;
            }

            return null;
        }
    }
}
#endif