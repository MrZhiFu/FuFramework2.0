//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FuFramework.Core.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

namespace GameFrameX.Editor
{
    /// <summary>
    /// 类型相关的实用函数。
    /// </summary>
    public static class Type
    {
        /// 运行时程序集名称列表
        private static readonly string[] s_RuntimeAssemblyNames = Utility.Assembly.GetAssemblies().Where(m => !m.FullName.Contains("Editor")).Select(m => m.FullName).ToArray();
        
        /// 运行时或编辑器程序集名称列表
        private static readonly string[] s_RuntimeOrEditorAssemblyNames = Utility.Assembly.GetAssemblies().Select(m => m.FullName).ToArray();

        /// <summary>
        /// 在运行时程序集中获取指定基类的所有子类的名称。
        /// </summary>
        /// <param name="typeBase">基类类型。</param>
        /// <returns>指定基类的所有子类的名称。</returns>
        public static string[] GetRuntimeTypeNames(System.Type typeBase)
        {
            return GetTypeNames(typeBase, s_RuntimeAssemblyNames);
        }

        /// <summary>
        /// 在运行时或编辑器程序集中获取指定基类的所有子类的名称。
        /// </summary>
        /// <param name="typeBase">基类类型。</param>
        /// <returns>指定基类的所有子类的名称。</returns>
        internal static string[] GetRuntimeOrEditorTypeNames(System.Type typeBase)
        {
            return GetTypeNames(typeBase, s_RuntimeOrEditorAssemblyNames);
        }

        /// <summary>
        /// 获取指定基类的所有子类的名称。
        /// </summary>
        /// <param name="typeBase"></param>
        /// <param name="assemblyNames"></param>
        /// <returns></returns>
        private static string[] GetTypeNames(System.Type typeBase, string[] assemblyNames)
        {
            var typeNames = new List<string>();
            foreach (var assemblyName in assemblyNames)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch
                {
                    continue;
                }

                if (assembly == null) continue;

                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (!type.IsClass || type.IsAbstract || !typeBase.IsAssignableFrom(type)) continue;
                    typeNames.Add(type.FullName);
                }
            }

            typeNames.Sort();
            return typeNames.ToArray();
        }
    }
}