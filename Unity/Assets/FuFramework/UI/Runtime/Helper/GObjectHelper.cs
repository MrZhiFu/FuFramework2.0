using FairyGUI;
using System.Collections.Generic;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// GObject 帮助类
    /// </summary>
    
    public static class GObjectHelper
    {
        /// <summary>
        /// 界面组件池
        /// </summary>
        private static readonly Dictionary<GComponent, ViewBase> GObject2UIDict = new();

        /// <summary>
        /// 从组件池中获取UI对象
        /// </summary>
        /// <param name="self"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>UI对象</returns>
        public static T Get<T>(this GComponent self) where T : ViewBase
        {
            if (self != null && GObject2UIDict.TryGetValue(self, out var pair)) 
                return pair as T;
            
            return null;
        }

        /// <summary>
        /// 添加UI对象到组件池
        /// </summary>
        /// <param name="self"></param>
        /// <param name="fui">UI对象</param>
        public static void Add(this GComponent self, ViewBase fui)
        {
            if (self == null || fui == null) return;
            GObject2UIDict[self] = fui;
        }

        /// <summary>
        /// 从组件池中删除UI对象。返回删除的UI对象
        /// </summary>
        /// <param name="self"></param>
        /// <returns>UI对象</returns>
        public static ViewBase Remove(this GComponent self)
        {
            if (self != null && GObject2UIDict.Remove(self, out var value)) return value;
            return null;
        }
    }
}