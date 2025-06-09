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
        public bool HasUI(int serialId)
        {
            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUI(serialId)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUI(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUI(uiFormAssetName)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUI(int serialId)
        {
            foreach (var (_, group) in m_UIGroupDict)
            {
                var uiForm = group.GetUI(serialId);
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
        public IUIForm GetUI(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                var uiForm = group.GetUI(uiFormAssetName);
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
        public IUIForm[] GetUIs(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));

            var results = new List<IUIForm>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetUIs(uiFormAssetName));
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有界面组下的界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIs(string uiFormAssetName, List<IUIForm> results)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));
            GameFrameworkGuard.NotNull(results, nameof(results));
            
            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIs());
            }
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        public IUIForm[] GetAllLoadedUIs()
        {
            var results = new List<IUIForm>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIs());
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        public void GetAllLoadedUIs(List<IUIForm> results)
        {
            GameFrameworkGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIs());
            }
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUISerialIds()
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
        public void GetAllLoadingUISerialIds(List<int> results)
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
        public bool IsLoadingUI(int serialId) => m_LoadingDict.ContainsKey(serialId);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUI(string uiFormAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiFormAssetName, nameof(uiFormAssetName));
            return m_LoadingDict.ContainsValue(uiFormAssetName);
        }

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUI(IUIForm uiForm)
        {
            return uiForm != null && HasUI(uiForm.SerialId);
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="fullName">完整界面名称</param>
        /// <returns></returns>
        public bool HasUIFullName(string fullName)
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