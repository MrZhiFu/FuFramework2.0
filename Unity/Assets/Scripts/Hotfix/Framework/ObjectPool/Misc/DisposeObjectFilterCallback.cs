using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 销毁对象筛选函数。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="candidateObjects">要筛选的对象候选集合。</param>
    /// <param name="toDisposeCount">需要销毁的对象数量。</param>
    /// <param name="expireTimeThreshold">对象过期时间点(为空时表示不限制过期时间点)。</param>
    /// <returns>经筛选需要销毁的对象集合。</returns>
    public delegate List<T> DisposeObjectFilterCallback<T>(List<T> candidateObjects, int toDisposeCount, DateTime? expireTimeThreshold) where T : ObjectBase;
}
