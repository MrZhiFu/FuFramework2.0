# FuFramework Procedure Module

## 1. 简介

FuFramework Procedure 模块是游戏框架的流程管理系统，基于有限状态机（FSM）实现游戏流程的状态管理和转换。该模块提供了一种结构化的方式来管理游戏的不同阶段和流程，如启动流程、登录流程、游戏主流程等。

## 2. 核心特性

- **基于 FSM**：继承自有限状态机模块，提供成熟的状态管理能力
- **流程管理**：统一管理游戏各个阶段的流程转换
- **生命周期**：完整的流程生命周期管理（初始化、进入、更新、离开）
- **优先级控制**：支持流程优先级设置（用于编辑器展示）
- **状态监控**：实时监控当前流程状态和持续时间
- **类型安全**：泛型接口确保流程类型安全

## 3. 核心概念

### 3.1 类继承与实现体系

```
【类继承体系】

FuModule (框架模块基类)
    └── ProcedureModule (流程管理模块)

FsmStateBase (状态机状态基类)
    └── ProcedureBase (流程基类)
        └── 用户自定义流程类 (如 LaunchProcedure, GameProcedure)

【依赖关系】

ProcedureModule 依赖:
    └── FsmModule (有限状态机模块)
        └── Fsm (状态机实例)
            └── FsmStateBase[] (状态集合)
                └── ProcedureBase[] (流程集合)

【模块依赖特性】

[ModuleDependency(typeof(FsmModule))]
public sealed class ProcedureModule : FuModule
    └── 确保 FsmModule 先于 ProcedureModule 初始化
```

### 3.2 流程架构

```
┌─────────────────────────────────────────────────────────────┐
│                   ProcedureModule                           │
│                      (FuModule)                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                   m_ProcedureFsm                      │   │
│  │                      (Fsm)                            │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │   │
│  │  │ LaunchProcedure│ LoginProcedure│ GameProcedure│   │   │
│  │  │ (ProcedureBase)│ (ProcedureBase)│ (ProcedureBase)│   │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘   │   │
│  │                                                       │   │
│  │  CurrentStateBase ──────▶ CurrentProcedure            │   │
│  │  CurrentStateTime  ──────▶ CurrentProcedureTime       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │    FsmModule     │
                    │   (依赖模块)      │
                    └──────────────────┘
```

### 3.3 流程生命周期

```
【流程状态流转】

初始化 (InitProcedures)
    │
    ▼
┌─────────┐     进入 (OnEnter)     ┌─────────┐     更新 (OnUpdate)    ┌─────────┐
│  待机   │ ─────────────────────▶ │  运行中 │ ─────────────────────▶ │  持续   │
│ (Idle)  │                        │ (Active)│                        │ (Update)│
└─────────┘                        └─────────┘                        └────┬────┘
    ▲                                                                      │
    │                                                                      │
    │ 离开 (OnLeave)                                                       │
    └──────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                        ┌─────────┐
                        │  结束   │
                        │ (Leave) │
                        └─────────┘

【生命周期回调顺序】

流程切换: ProcedureA ──▶ ProcedureB

ProcedureA.OnLeave(isShutdown)     // 先离开当前流程
        │
        ▼
ProcedureB.OnEnter(userData)       // 再进入新流程
        │
        ▼
ProcedureB.OnUpdate(...)           // 持续更新
```

### 3.4 与 FSM 模块的关系

```
【Procedure 与 Fsm 的映射关系】

ProcedureModule                          FsmModule
    │                                        │
    ├── InitProcedures(procedures)  ──────▶  ├── CreateFsm(owner, states)
    │                                        │
    ├── StartProcedure<T>()         ──────▶  ├── Start<T>()
    │                                        │
    ├── HasProcedure<T>()           ──────▶  ├── HasState<T>()
    │                                        │
    ├── GetProcedure<T>()           ──────▶  ├── GetState<T>()
    │                                        │
    └── CurrentProcedure            ──────▶  └── CurrentStateBase

【ProcedureBase 与 FsmStateBase 的映射关系】

ProcedureBase                            FsmStateBase
    │                                        │
    ├── OnInit(procedureOwner)      ──────▶  ├── OnInit(fsm)
    │                                        │
    ├── OnEnter(userData)           ──────▶  ├── OnEnter(userData)
    │                                        │
    ├── OnUpdate(...)               ──────▶  ├── OnUpdate(...)
    │                                        │
    ├── OnLeave(isShutdown)         ──────▶  ├── OnLeave(isShutdown)
    │                                        │
    └── ChangeProcedure<T>()        ──────▶  └── ChangeState<T>()
```

## 4. 核心类详细说明

### 4.1 ProcedureModule

流程管理模块，继承自 `FuModule`，负责管理所有流程。

**核心功能：**

```csharp
[ModuleDependency(typeof(FsmModule))]
public sealed class ProcedureModule : FuModule
{
    // 流程状态监控
    public ProcedureBase CurrentProcedure { get; }      // 获取当前流程
    public float CurrentProcedureTime { get; }          // 获取当前流程持续时间
    
    // 流程状态机管理
    public void InitProcedures(ProcedureBase[] procedures)  // 初始化流程状态机
    
    // 流程控制
    public void StartProcedure<T>() where T : ProcedureBase   // 开始指定流程
    public void StartProcedure(Type procedureType)            // 通过类型开始流程
    
    // 流程查询
    public bool HasProcedure<T>() where T : ProcedureBase     // 检查是否存在流程
    public bool HasProcedure(Type procedureType)              // 通过类型检查
    public ProcedureBase GetProcedure<T>() where T : ProcedureBase  // 获取流程
    public ProcedureBase GetProcedure(Type procedureType)     // 通过类型获取
}
```

**模块依赖：**
- 依赖 `FsmModule`，通过 `[ModuleDependency]` 特性确保初始化顺序
- 内部维护一个 `Fsm` 实例来管理流程状态

### 4.2 ProcedureBase

流程基类，继承自 `FsmStateBase`，所有自定义流程都需要继承此类。

**核心功能：**

```csharp
public abstract class ProcedureBase : FsmStateBase
{
    // 优先级（用于编辑器展示排序）
    public virtual int Priority => 0;
    
    // 生命周期方法（继承自 FsmStateBase）
    protected override void OnInit(Fsm.Runtime.Fsm procedureOwner)   // 流程初始化
    protected override void OnEnter(object userData)                 // 进入流程
    protected override void OnUpdate(float elapseSeconds,            // 流程更新
                                     float realElapseSeconds)
    protected override void OnLeave(bool isShutdown)                 // 离开流程
    
    // 流程切换（继承自 FsmStateBase）
    protected void ChangeProcedure<T>() where T : ProcedureBase      // 切换到指定流程
    protected void ChangeProcedure(Type procedureType)               // 通过类型切换
}
```

**继承的方法：**
- `CurrentProcedureTime` - 当前流程持续时间
- `Fsm.Owner` - 获取流程持有者（ProcedureModule）

## 5. 使用示例

### 5.1 定义自定义流程类

```csharp
using FuFramework.Procedure.Runtime;
using FuFramework.Fsm.Runtime;
using UnityEngine;

/// <summary>
/// 启动流程 - 游戏启动时执行，负责初始化游戏系统
/// </summary>
public class LaunchProcedure : ProcedureBase
{
    // 设置优先级（数值越大越靠前，用于编辑器展示）
    public override int Priority =>