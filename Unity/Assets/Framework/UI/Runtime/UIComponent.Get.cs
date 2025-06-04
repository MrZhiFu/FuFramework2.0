using System.Collections.Generic;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUIForm(int serialId)
        {
            return m_UIManager.GetUIForm(serialId);
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUIForm(string uiFormAssetName)
        {
            return m_UIManager.GetUIForm(uiFormAssetName);
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm[] GetUIForms(string uiFormAssetName)
        {
            var uiForms = m_UIManager.GetUIForms(uiFormAssetName);
            var uiFormImpls = new IUIForm[uiForms.Length];
            for (int i = 0; i < uiForms.Length; i++)
            {
                uiFormImpls[i] = uiForms[i];
            }

            return uiFormImpls;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIForms(string uiFormAssetName, List<IUIForm> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_UIManager.GetUIForms(uiFormAssetName, m_InternalUIFormResults);
            foreach (var uiForm in m_InternalUIFormResults)
            {
                results.Add(uiForm);
            }
        }

        /// <summary>
        /// 根据界面逻辑类型获取界面列表,会返回所有符合条件的集合
        /// </summary>
        /// <typeparam name="T">界面逻辑类型</typeparam>
        /// <returns></returns>
        public List<T> GetLoadedList<T>() where T : class, IUIForm
        {
            var fullName = typeof(T).FullName;
            var uiForms = m_UIManager.GetAllLoadedUIForms();
            var results = new List<T>();
            foreach (var uiForm in uiForms)
            {
                if (uiForm.FullName == fullName)
                {
                    results.Add(uiForm as T);
                }
            }

            return results;
        }


        /// <summary>
        /// 获取已加载且正在显示的UI。
        /// </summary>
        /// <typeparam name="T">UI的具体类型。</typeparam>
        /// <returns>返回已加载且正在显示的UI实例，如果未找到则返回null。</returns>
        public T GetLoadedAndShowing<T>() where T : class, IUIForm
        {
            var fullName = typeof(T).FullName;
            var uiForms = m_UIManager.GetAllLoadedUIForms();
            foreach (var uiForm in uiForms)
            {
                if (uiForm.FullName == fullName && uiForm.Visible && uiForm.Available)
                {
                    return uiForm as T;
                }
            }

            return null;
        }


        /// <summary>
        /// 根据界面逻辑类型获取界面。只要找到任意的一个即返回
        /// </summary>
        /// <typeparam name="T">逻辑界面类型</typeparam>
        /// <returns></returns>
        public T GetLoaded<T>() where T : class, IUIForm
        {
            var fullName = typeof(T).FullName;
            var uiForms = m_UIManager.GetAllLoadedUIForms();
            foreach (var uiForm in uiForms)
            {
                if (uiForm.FullName == fullName)
                {
                    return uiForm as T;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        public IUIForm[] GetAllLoadedUIForms()
        {
            var uiForms = m_UIManager.GetAllLoadedUIForms();
            var uiFormImpls = new IUIForm[uiForms.Length];
            for (int i = 0; i < uiForms.Length; i++)
            {
                uiFormImpls[i] = uiForms[i];
            }

            return uiFormImpls;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        public void GetAllLoadedUIForms(List<IUIForm> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_UIManager.GetAllLoadedUIForms(m_InternalUIFormResults);
            foreach (var uiForm in m_InternalUIFormResults)
            {
                results.Add(uiForm);
            }
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUIFormSerialIds()
        {
            return m_UIManager.GetAllLoadingUIFormSerialIds();
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载界面的序列编号。</param>
        public void GetAllLoadingUIFormSerialIds(List<int> results)
        {
            m_UIManager.GetAllLoadingUIFormSerialIds(results);
        }
    }
}