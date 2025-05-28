﻿using System.Reflection;

namespace Hotfix.Proto
{
    /// <summary>
    /// 这个类是用来标记协议程序集的。
    /// </summary>
    public static class HotfixProtoHandler
    {
        /// <summary>
        /// 当前程序集
        /// </summary>
        public static Assembly CurrentAssembly => typeof(HotfixProtoHandler).Assembly;
    }
}