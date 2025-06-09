﻿using FairyGUI;
using System.Collections.Generic;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// GObject 帮助类
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public static class GObjectHelper
    {
        /// <summary>
        /// 界面组件池
        /// </summary>
        private static readonly Dictionary<GObject, FUI> GObject2UIDict = new();

        /// <summary>
        /// 从组件池中获取UI对象
        /// </summary>
        /// <param name="self"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>UI对象</returns>
        public static T Get<T>(this GObject self) where T : FUI
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
        public static void Add(this GObject self, FUI fui)
        {
            if (self == null || fui == null) return;
            GObject2UIDict[self] = fui;
        }

        /// <summary>
        /// 从组件池中删除UI对象。返回删除的UI对象
        /// </summary>
        /// <param name="self"></param>
        /// <returns>UI对象</returns>
        public static FUI Remove(this GObject self)
        {
            if (self != null && GObject2UIDict.Remove(self, out var value)) return value;
            return null;
        }
    }
}