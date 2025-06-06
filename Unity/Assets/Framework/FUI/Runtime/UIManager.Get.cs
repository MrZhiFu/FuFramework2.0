//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Asset.Runtime;
using GameFrameX.ObjectPool;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(int serialId)
        {
            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUIForm(serialId)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUIForm(uiFormAssetName)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUIForm(int serialId)
        {
            foreach (var (_, group) in m_UIGroupDict)
            {
                var uiForm = group.GetUIForm(serialId);
                if (uiForm == null) continue;
                return uiForm;
            }

            return null;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUIForm(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                var uiForm = group.GetUIForm(uiFormAssetName);
                if (uiForm == null) continue;
                return uiForm;
            }

            return null;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm[] GetUIForms(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));

            var results = new List<IUIForm>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetUIForms(uiFormAssetName));
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有界面组下的界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIForms(string uiFormAssetName, List<IUIForm> results)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));
            GameFrameworkGuard.NotNull(results, nameof(results));
            
            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIForms());
            }
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        public IUIForm[] GetAllLoadedUIForms()
        {
            var results = new List<IUIForm>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIForms());
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        public void GetAllLoadedUIForms(List<IUIForm> results)
        {
            GameFrameworkGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIForms());
            }
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUIFormSerialIds()
        {
            var index = 0;
            var results = new int[m_LoadingDict.Count];
            foreach (var (id, _) in m_LoadingDict)
            {
                results[index++] = id;
            }

            return results;
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载界面的序列编号。</param>
        public void GetAllLoadingUIFormSerialIds(List<int> results)
        {
            GameFrameworkGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (id, _) in m_LoadingDict)
            {
                results.Add(id);
            }
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(int serialId) => m_LoadingDict.ContainsKey(serialId);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));
            return m_LoadingDict.ContainsValue(uiFormAssetName);
        }

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUIForm(IUIForm uiForm)
        {
            return uiForm != null && HasUIForm(uiForm.SerialId);
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="fullName">完整界面名称</param>
        /// <returns></returns>
        public bool HasUIFormFullName(string fullName)
        {
            GameFrameworkGuard.NotNullOrEmpty(fullName, nameof(fullName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUIFormFullName(fullName)) continue;
                return true;
            }

            return false;
        }
    }
}