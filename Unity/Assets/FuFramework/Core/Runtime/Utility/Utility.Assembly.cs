//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using UnityEngine.Scripting;

namespace GameFrameX.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 程序集相关的实用函数。
        /// 1.获取已加载的程序集。
        /// 2.获取已加载的程序集中的所有类型。
        /// 3.获取已加载的程序集中的指定类型。
        /// 4.获取已加载的程序集中的指定类型的子类列表。
        /// </summary>
        public static class Assembly
        {
            /// <summary>
            /// 当前域中已加载的所有程序集
            /// </summary>
            private static readonly System.Reflection.Assembly[] s_Assemblies;

            /// <summary>
            /// 缓存类型的字典，key为类型名，value为类型
            /// </summary>
            private static readonly Dictionary<string, Type> s_CachedDict = new(StringComparer.Ordinal);

            static Assembly()
            {
                s_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            /// <summary>
            /// 获取已加载的程序集。
            /// </summary>
            /// <returns>已加载的程序集。</returns>
            public static System.Reflection.Assembly[] GetAssemblies() => s_Assemblies;

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <returns>已加载的程序集中的所有类型。</returns>
            public static Type[] GetTypes()
            {
                var results = new List<Type>();
                foreach (var assembly in s_Assemblies)
                {
                    results.AddRange(assembly.GetTypes());
                }

                return results.ToArray();
            }

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <param name="results">已加载的程序集中的所有类型。</param>
            public static void GetTypes(List<Type> results)
            {
                if (results == null)
                    throw new GameFrameworkException("传入的结果列表为空，请检查参数是否正确.");

                results.Clear();
                foreach (var assembly in s_Assemblies)
                {
                    results.AddRange(assembly.GetTypes());
                }
            }

            /// <summary>
            /// 获取已加载的程序集中的指定类型。
            /// </summary>
            /// <param name="typeName">要获取的类型名。</param>
            /// <returns>已加载的程序集中的指定类型。</returns>
            public static Type GetType(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                    throw new GameFrameworkException("传入的类型名为空，请检查参数是否正确.");

                if (s_CachedDict.TryGetValue(typeName, out var type)) return type;

                type = Type.GetType(typeName);
                if (type != null)
                {
                    s_CachedDict.Add(typeName, type);
                    return type;
                }

                foreach (var assembly in s_Assemblies)
                {
                    type = Type.GetType(Text.Format("{0}, {1}", typeName, assembly.FullName));
                    if (type == null) continue;
                    s_CachedDict.Add(typeName, type);
                    return type;
                }

                return null;
            }

            /// <summary>
            /// 获取已加载的程序集中的指定类型的子类列表。
            /// </summary>
            /// <param name="type">指定类型</param>
            /// <returns></returns>
            public static List<string> GetRuntimeTypeNames(Type type)
            {
                var types   = GetTypes();
                var results = new List<string>();
                foreach (var t in types)
                {
                    if (t.IsAbstract || !t.IsClass) continue;
                    if (t.IsSubclassOf(type) || t.IsImplWithInterface(type))
                    {
                        results.Add(t.FullName);
                    }
                }

                return results;
            }
        }
    }
}