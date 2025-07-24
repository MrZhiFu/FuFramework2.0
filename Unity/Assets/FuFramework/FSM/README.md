## HOMEPAGE
GameFrameX 的 Fsm 有限状态机组件

 **Fsm 有限状态机组件 (Fsm Component)** - 提供状态机组件相关的接口。


# 使用文档(文档编写于GPT4)
# 有限状态机组件 `FsmComponent` 说明文档

## 简介
`FsmComponent` 是一个集成于Game Framework游戏框架的有限状态机组件，用于管理和控制有限状态机（FSM）的创建、获取、检查以及销毁。

## 功能
- `Count` 属性：获取当前状态机的数量。
- `HasFsm` 方法：检查指定类型的状态机是否已经存在。
- `GetFsm` 方法：根据指定类型获取状态机实例。
- `GetAllFsmList` 方法：获取所有状态机实例。
- `CreateFsm` 方法：创建新的状态机实例。
- `DestroyFsm` 方法：销毁指定的状态机实例。

## 使用方法

### 初始化组件
在游戏开始时，`Awake` 方法会被自动调用。

```csharp
protected override void Awake()
{
    base.Awake();
    new FsmManager();
    m_FsmManager = GameFrameworkEntry.GetModule<IFsmManager>();
    if (m_FsmManager == null)
    {
        Log.Fatal("FSM manager is invalid.");
        return;
    }
}
```

### 创建状态机
使用 `CreateFsm` 方法创建一个新的有限状态机。需要提供拥有者对象、状态机名称（可选）和状态集合。

```csharp
public IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class
{
    return m_FsmManager.CreateFsm(owner, states);
}
```

### 获取状态机
根据拥有者类型或名称来获取对应的有限状态机。

```csharp
public IFsm<T> GetFsm<T>() where T : class
{
    return m_FsmManager.GetFsm<T>();
}
```

### 检查状态机存在
调用 `HasFsm` 方法确认是否已创建特定的有限状态机。

```csharp
public bool HasFsm<T>() where T : class
{
    return m_FsmManager.HasFsm<T>();
}
```

### 销毁状态机
使用 `DestroyFsm` 方法销毁不再需要的状态机，回收资源。

```csharp
public bool DestroyFsm<T>(IFsm<T> fsm) where T : class
{
    return m_FsmManager.DestroyFsm(fsm);
}
```

## 注意事项
确保在调用任何状态机管理方法之前，状态机管理器 `m_FsmManager` 已被正确初始化，否则可能会引发错误。

此组件需要与游戏框架的其他模块和组件进行交互使用，需保证游戏框架已被正确设置并初始化。

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.fsm": "https://github.com/AlianBlank/com.gameframex.unity.fsm.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.fsm.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别