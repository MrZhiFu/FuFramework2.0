#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using FuMenuPriority   = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase  = FuFramework.Core.Editor.DebugWindowBase;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Editor
{
	/// <summary>
	/// 计时器调试面板的反射缓存与数据刷新分部类。
	/// 职责：Hotfix 类型/成员反射缓存（EnsureReflection/ResetReflection）、每次刷新重建
	/// 模块实例与信息/统计快照（OnRefresh），与主体绘制分部类（TimerModuleWindow.cs）分离。
	/// </summary>
	public sealed partial class TimerModuleWindow
	{
		#region 反射缓存

		/// <summary>
		/// TimerInfo 类型
		/// </summary>
		private System.Type m_timerInfoType;

		/// <summary>
		/// TimerModule.GetStatistics 方法
		/// </summary>
		private MethodInfo m_getStatisticsMethod;

		/// <summary>
		/// TimerModule.GetAllTimerInfos 方法
		/// </summary>
		private MethodInfo m_getAllTimerInfosMethod;

		/// <summary>
		/// 单计时器操作方法（Pause/Resume/Reset/Stop）
		/// </summary>
		private MethodInfo m_pauseTimerMethod;
		private MethodInfo m_resumeTimerMethod;
		private MethodInfo m_resetTimerMethod;
		private MethodInfo m_stopTimerMethod;

		/// <summary>
		/// 批量操作方法（Pause/Resume/Reset/Stop All）
		/// </summary>
		private MethodInfo m_pauseAllMethod;
		private MethodInfo m_resumeAllMethod;
		private MethodInfo m_resetAllMethod;
		private MethodInfo m_stopAllMethod;

		/// <summary>
		/// TimerModuleStatistics 四个计数字段属性
		/// </summary>
		private PropertyInfo m_statStartedProperty;
		private PropertyInfo m_statFinishedProperty;
		private PropertyInfo m_statStoppedProperty;
		private PropertyInfo m_statErrorProperty;

		/// <summary>
		/// TimerInfo 各属性
		/// </summary>
		private PropertyInfo m_infoIdProperty;
		private PropertyInfo m_infoTypeProperty;
		private PropertyInfo m_infoIsPausedProperty;
		private PropertyInfo m_infoIgnoreTimeScaleProperty;
		private PropertyInfo m_infoImmediatePendingProperty;
		private PropertyInfo m_infoExecutedCountProperty;
		private PropertyInfo m_infoMaxCountProperty;
		private PropertyInfo m_infoRemainingTimeProperty;
		private PropertyInfo m_infoDurationTimeProperty;
		private PropertyInfo m_infoIntervalProperty;
		private PropertyInfo m_infoAccumulatedProperty;
		private PropertyInfo m_infoStartStackTraceProperty;

		#endregion

		#region 刷新与反射

		/// <summary>
		/// 重建模块实例、信息快照与统计计数
		/// </summary>
		protected override void OnRefresh()
		{
			m_moduleInstance = HotfixReflection.GetModuleInstance(HotfixReflection.TimerModule);
			if (m_moduleInstance == null)
			{
				m_infos.Clear();
				return;
			}

			// 信息快照：装箱数组逐元素收集，并按搜索框过滤（Id/类型标签/Name 任一命中）
			m_infos.Clear();
			m_typeCounts[0] = 0;
			m_typeCounts[1] = 0;
			m_typeCounts[2] = 0;
			m_pausedCount   = 0;
			var infosResult = m_getAllTimerInfosMethod?.Invoke(m_moduleInstance, null) as IEnumerable;
			if (infosResult != null)
			{
				foreach (var item in infosResult)
				{
					if (item == null) continue;

					m_infos.Add(item);
					var typeName = GetTimerTypeLabel(m_infoTypeProperty?.GetValue(item));
					var index    = typeName == "一次性倒计时" ? 0 : typeName == "时间间隔" ? 1 : 2;
					m_typeCounts[index]++;
					if ((bool)(m_infoIsPausedProperty?.GetValue(item) ?? false)) m_pausedCount++;
				}
			}

			// 生命周期统计
			var stats = m_getStatisticsMethod?.Invoke(m_moduleInstance, null);
			if (stats != null)
			{
				m_statStarted  = (int)(m_statStartedProperty?.GetValue(stats)  ?? 0);
				m_statFinished = (int)(m_statFinishedProperty?.GetValue(stats) ?? 0);
				m_statStopped  = (int)(m_statStoppedProperty?.GetValue(stats)  ?? 0);
				m_statError    = (int)(m_statErrorProperty?.GetValue(stats)    ?? 0);
			}
		}

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_timerInfoType != null) return true;

			var moduleType = HotfixReflection.TimerModule;
			if (moduleType == null) return false;

			m_timerInfoType       = HotfixReflection.TimerInfo;

			const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

			m_getStatisticsMethod       = moduleType.GetMethod("GetStatistics", PublicInstance);
			m_getAllTimerInfosMethod    = moduleType.GetMethod("GetAllTimerInfos", PublicInstance);
			m_pauseTimerMethod          = moduleType.GetMethod("PauseTimer", PublicInstance);
			m_resumeTimerMethod         = moduleType.GetMethod("ResumeTimer", PublicInstance);
			m_resetTimerMethod          = moduleType.GetMethod("ResetTimer", PublicInstance);
			m_stopTimerMethod           = moduleType.GetMethod("StopTimer", PublicInstance);
			m_pauseAllMethod            = moduleType.GetMethod("PauseAllTimers", PublicInstance);
			m_resumeAllMethod           = moduleType.GetMethod("ResumeAllTimers", PublicInstance);
			m_resetAllMethod            = moduleType.GetMethod("ResetAllTimers", PublicInstance);
			m_stopAllMethod             = moduleType.GetMethod("StopAllTimers", PublicInstance);

			if (m_timerInfoType == null) return false;
			m_infoIdProperty               = m_timerInfoType.GetProperty("Id", PublicInstance);
			m_infoTypeProperty             = m_timerInfoType.GetProperty("Type", PublicInstance);
			m_infoIsPausedProperty         = m_timerInfoType.GetProperty("IsPaused", PublicInstance);
			m_infoIgnoreTimeScaleProperty  = m_timerInfoType.GetProperty("IgnoreTimeScale", PublicInstance);
			m_infoImmediatePendingProperty = m_timerInfoType.GetProperty("ImmediatePending", PublicInstance);
			m_infoExecutedCountProperty    = m_timerInfoType.GetProperty("ExecutedCount", PublicInstance);
			m_infoMaxCountProperty         = m_timerInfoType.GetProperty("MaxCount", PublicInstance);
			m_infoRemainingTimeProperty    = m_timerInfoType.GetProperty("RemainingTime", PublicInstance);
			m_infoDurationTimeProperty     = m_timerInfoType.GetProperty("DurationTime", PublicInstance);
			m_infoIntervalProperty         = m_timerInfoType.GetProperty("Interval", PublicInstance);
			m_infoAccumulatedProperty      = m_timerInfoType.GetProperty("Accumulated", PublicInstance);
			m_infoStartStackTraceProperty  = m_timerInfoType.GetProperty("StartStackTrace", PublicInstance);

			var statisticsType = m_getStatisticsMethod?.ReturnType;
			m_statStartedProperty  = statisticsType?.GetProperty("StartedCount", PublicInstance);
			m_statFinishedProperty = statisticsType?.GetProperty("FinishedCount", PublicInstance);
			m_statStoppedProperty  = statisticsType?.GetProperty("StoppedCount", PublicInstance);
			m_statErrorProperty    = statisticsType?.GetProperty("ErrorCount", PublicInstance);

			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_moduleInstance = null;
			m_infos.Clear();
			m_timerInfoType         = null;
			m_getStatisticsMethod   = null;
			m_getAllTimerInfosMethod = null;
			m_pauseTimerMethod  = null;
			m_resumeTimerMethod = null;
			m_resetTimerMethod  = null;
			m_stopTimerMethod   = null;
			m_pauseAllMethod    = null;
			m_resumeAllMethod   = null;
			m_resetAllMethod    = null;
			m_stopAllMethod     = null;
			m_infoIdProperty               = null;
			m_infoTypeProperty             = null;
			m_infoIsPausedProperty         = null;
			m_infoIgnoreTimeScaleProperty  = null;
			m_infoImmediatePendingProperty = null;
			m_infoExecutedCountProperty    = null;
			m_infoMaxCountProperty         = null;
			m_infoRemainingTimeProperty    = null;
			m_infoDurationTimeProperty     = null;
			m_infoIntervalProperty         = null;
			m_infoAccumulatedProperty      = null;
			m_infoStartStackTraceProperty  = null;
			m_statStartedProperty  = null;
			m_statFinishedProperty = null;
			m_statStoppedProperty  = null;
			m_statErrorProperty    = null;
		}

		#endregion
	}
}
#endif
