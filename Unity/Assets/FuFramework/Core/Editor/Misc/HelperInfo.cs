using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 各个帮助器Inspector的信息显示
    /// </summary>
    /// <typeparam name="T"> 帮助器类型 </typeparam>
    public sealed class HelperInfo<T> where T : MonoBehaviour
    {
        /// <summary>
        /// 自定义选项名称
        /// </summary>
        private const string CustomOptionName = "<Custom>";

        /// <summary>
        /// 帮助器名称
        /// </summary>
        private readonly string m_Name;

        /// <summary>
        /// 帮助器类型名称
        /// </summary>
        private SerializedProperty m_HelperTypeName;
        
        /// <summary>
        /// 帮助器类型名称数组
        /// </summary>
        private string[] m_HelperTypeNames;
        
        /// <summary>
        /// 帮助器类型名称索引
        /// </summary>
        private int m_HelperTypeNameIndex;
        
        /// <summary>
        /// 自定义帮助器
        /// </summary>
        private SerializedProperty m_CustomHelper;

        public HelperInfo(string name)
        {
            m_Name = name;

            m_HelperTypeName = null;
            m_CustomHelper = null;
            m_HelperTypeNames = null;
            m_HelperTypeNameIndex = 0;
        }

        public void Init(SerializedObject serializedObject)
        {
            m_HelperTypeName = serializedObject.FindProperty(Utility.Text.Format("m_{0}HelperTypeName", m_Name));
            m_CustomHelper = serializedObject.FindProperty(Utility.Text.Format("m_Custom{0}Helper", m_Name));
        }

        public void Draw()
        {
            var displayName = FieldNameForDisplay(m_Name);
            var selectedIndex = EditorGUILayout.Popup(Utility.Text.Format("{0} Helper", displayName), m_HelperTypeNameIndex, m_HelperTypeNames);
            if (selectedIndex != m_HelperTypeNameIndex)
            {
                m_HelperTypeNameIndex = selectedIndex;
                m_HelperTypeName.stringValue = selectedIndex <= 0 ? null : m_HelperTypeNames[selectedIndex];
            }

            if (m_HelperTypeNameIndex > 0) return;
            
            // 使用自定义帮助器
            EditorGUILayout.PropertyField(m_CustomHelper);
            if (m_CustomHelper.objectReferenceValue != null) return;
            EditorGUILayout.HelpBox(Utility.Text.Format("你必须选择{0}帮助器类型.", displayName), MessageType.Error);
        }

        /// <summary>
        /// 刷新帮助器信息
        /// </summary>
        public void Refresh()
        {
            var helperTypeNameList = new List<string> { CustomOptionName };

            helperTypeNameList.AddRange(Utility.Assembly.GetRuntimeTypeNames(typeof(T)));
            m_HelperTypeNames = helperTypeNameList.ToArray();

            m_HelperTypeNameIndex = 0;
            if (string.IsNullOrEmpty(m_HelperTypeName.stringValue)) return;
            
            m_HelperTypeNameIndex = helperTypeNameList.IndexOf(m_HelperTypeName.stringValue);
            if (m_HelperTypeNameIndex > 0) return;
            m_HelperTypeNameIndex        = 0;
            m_HelperTypeName.stringValue = null;
        }

        /// <summary>
        /// 字段名称转为显示名称。如：m_MyHelperName -> My Helper Name
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private static string FieldNameForDisplay(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return string.Empty;

            var str = Regex.Replace(fieldName, @"^m_", string.Empty);
            str = Regex.Replace(str, @"((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", @" $1").TrimStart();
            return str;
        }
    }
}