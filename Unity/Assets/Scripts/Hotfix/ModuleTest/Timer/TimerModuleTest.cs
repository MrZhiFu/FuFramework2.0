using Cysharp.Threading.Tasks;
using FuFramework.Entry.Runtime;
using FuFramework.Timer.Runtime;
using UnityEngine;

public class TimerModuleTest : MonoBehaviour
{
    private TimerManager m_TimerManager;
    private int m_TimerId1;
    private int m_TimerId2;
    private int m_IntervalTimerId;

    private void Start()
    {
        // 获取 TimerManager 实例（假设通过框架的模块系统获取）
        m_TimerManager = GlobalModule.TimerModule;
        
        // // 示例1：基本计时器使用
        // TestBasicTimer();
        
        // 示例2：带更新回调的计时器
        TestTimerWithUpdate();
        
        // // 示例3：循环间隔计时器
        // TestIntervalTimer();
        //
        // // 示例4：计时器控制操作
        // TestTimerControl();
    }

    /// <summary>
    /// 示例1：基本计时器使用
    /// </summary>
    private void TestBasicTimer()
    {
        m_TimerId1 = m_TimerManager.StartTimer(
            duration: 3f,
            finishCallBack: () =>
            {
                Debug.Log("3秒计时器完成！");
            }
        );
        
        Debug.Log($"启动基本计时器，ID: {m_TimerId1}");
    }

    /// <summary>
    /// 示例2：带更新回调的计时器
    /// </summary>
    private void TestTimerWithUpdate()
    {
        m_TimerId2 = m_TimerManager.StartTimer(
            duration: 5f,
            finishCallBack: () =>
            {
                Debug.Log("5秒计时器完成！");
            },
            updateCallBack: () =>
            {
                // 每帧更新时调用
                Debug.Log($"计时器更新中...");
            },
            playerLoopTiming: PlayerLoopTiming.Update,
            ignoreTimeScale: false
        );
        
        Debug.Log($"启动带更新回调的计时器，ID: {m_TimerId2}");
    }

    /// <summary>
    /// 示例3：循环间隔计时器
    /// </summary>
    private void TestIntervalTimer()
    {
        int counter = 0;
        m_IntervalTimerId = m_TimerManager.StartTimerInterval(
            interval: 2f,
            intervalCallback: () =>
            {
                counter++;
                Debug.Log($"间隔计时器触发，第{counter}次");
                
                // 执行5次后停止
                if (counter >= 5)
                {
                    m_TimerManager.StopTimer(m_IntervalTimerId);
                    Debug.Log("间隔计时器已停止");
                }
            },
            ignoreTimeScale: false
        );
        
        Debug.Log($"启动间隔计时器，ID: {m_IntervalTimerId}");
    }

    /// <summary>
    /// 示例4：计时器控制操作
    /// </summary>
    private void TestTimerControl()
    {
        // 3秒后暂停第一个计时器
        m_TimerManager.StartTimer(3f, () =>
        {
            if (m_TimerManager.IsTimerExist(m_TimerId1))
            {
                m_TimerManager.PauseTimer(m_TimerId1);
                Debug.Log($"计时器 {m_TimerId1} 已暂停");
                
                // 2秒后恢复计时器
                m_TimerManager.StartTimer(2f, () =>
                {
                    m_TimerManager.ResumeTimer(m_TimerId1);
                    Debug.Log($"计时器 {m_TimerId1} 已恢复");
                });
            }
        });

        // 10秒后停止所有计时器
        m_TimerManager.StartTimer(10f, () =>
        {
            Debug.Log("10秒后停止所有计时器");
            m_TimerManager.StopAllTimers();
        });
    }

    private void Update()
    {
        // 按键测试
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 暂停所有计时器
            m_TimerManager.PauseAllTimers();
            Debug.Log("暂停所有计时器");
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 恢复所有计时器
            m_TimerManager.ResumeAllTimers();
            Debug.Log("恢复所有计时器");
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            // 检查计时器状态
            Debug.Log($"计时器 {m_TimerId1} 是否存在: {m_TimerManager.IsTimerExist(m_TimerId1)}");
            Debug.Log($"计时器 {m_TimerId1} 是否暂停: {m_TimerManager.IsTimerPaused(m_TimerId1)}");
            Debug.Log($"当前活跃计时器数量: {m_TimerManager.Count}");
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            // 获取所有计时器名称
            var timerNames = m_TimerManager.GetAllTimerNames();
            foreach (var name in timerNames)
            {
                Debug.Log($"活跃计时器: {name}");
            }
        }
    }

    private void OnDestroy()
    {
        // 清理计时器
        if (m_TimerManager != null)
        {
            m_TimerManager.StopAllTimers();
        }
    }
}