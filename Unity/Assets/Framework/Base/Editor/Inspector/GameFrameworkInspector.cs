using UnityEditor;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 游戏框架 Inspector 抽象类。
    /// </summary>
    public abstract class GameFrameworkInspector : UnityEditor.Editor
    {
        /// <summary>
        /// 空选项名称。
        /// </summary>
        protected const string NoneOptionName = "<None>";

        /// <summary>
        /// 是否处于编译状态。
        /// </summary>
        private bool m_IsCompiling = false;

        /// <summary>
        /// 绘制事件。
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (m_IsCompiling && !EditorApplication.isCompiling)
            {
                m_IsCompiling = false;
                OnCompileComplete();
            }
            else if (!m_IsCompiling && EditorApplication.isCompiling)
            {
                m_IsCompiling = true;
                OnCompileStart();
            }
        }

        /// <summary>
        /// 编译开始事件。
        /// </summary>
        protected virtual void OnCompileStart() { }

        /// <summary>
        /// 编译完成事件。
        /// </summary>
        protected virtual void OnCompileComplete() { }


        /// <summary>
        /// 判断游戏对象是否是预制体。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected bool IsPrefabInHierarchy(UnityEngine.Object obj)
        {
            if (obj == null) return false;

            return PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.Regular;
        }
    }
}