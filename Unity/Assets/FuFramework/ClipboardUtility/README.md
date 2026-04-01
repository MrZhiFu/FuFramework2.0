# FuFramework OperationClipboard Module

## 概述

OperationClipboard 模块是 FuFramework 的跨平台剪贴板操作组件，提供统一的接口来读取和写入系统剪贴板内容，支持 Android、iOS、PC 和 WebGL 等多个平台。

### 核心特性

- **跨平台支持**：统一接口适配 Android、iOS、PC、WebGL 等平台
- **简单易用**：提供静态方法直接操作剪贴板
- **原生性能**：通过原生插件实现高性能剪贴板操作
- **线程安全**：安全的剪贴板读写操作
- **无依赖**：独立模块，不依赖其他 FuFramework 模块

## 核心类说明

### BlankOperationClipboard

剪贴板操作静态类，提供统一的剪贴板读写接口。

```csharp
public static class BlankOperationClipboard
```

**主要方法：**
- `GetValue()` - 获取剪贴板内容
- `SetValue(string text)` - 设置剪贴板内容

## 技术架构

### 平台适配实现

OperationClipboard 模块通过条件编译和原生插件实现跨平台支持：

#### Android 平台
- 使用 AndroidJavaClass 调用原生 Java 代码
- 通过 `com.alianhome.operationclipboard.MainActivity` 类实现剪贴板操作

#### iOS 平台
- 使用 P/Invoke 调用 Objective-C 原生代码
- 通过 `BlankOperationClipboard` 原生插件实现

#### PC/Standalone 平台
- 使用 Unity 的 `TextEditor` 类实现剪贴板操作
- 支持 Windows、Mac、Linux 等桌面平台

#### WebGL 平台
- 使用 Unity 的 `GUIUtility.systemCopyBuffer` 属性
- 浏览器环境下的剪贴板操作

### 依赖关系

```
BlankOperationClipboard → 平台原生API
```

**注意：** 本模块是独立模块，不依赖其他 FuFramework 模块。

## 使用指南

### 1. 基础使用

#### 获取剪贴板内容

```csharp
using FuFramework.OperationClipboard.Runtime;

// 获取剪贴板内容
string clipboardText = BlankOperationClipboard.GetValue();

if (!string.IsNullOrEmpty(clipboardText))
{
    Debug.Log($"剪贴板内容: {clipboardText}");
}
else
{
    Debug.Log("剪贴板为空");
}
```

#### 设置剪贴板内容

```csharp
using FuFramework.OperationClipboard.Runtime;

// 设置剪贴板内容
string textToCopy = "这是要复制到剪贴板的内容";
BlankOperationClipboard.SetValue(textToCopy);

Debug.Log("内容已复制到剪贴板");
```

### 2. 完整示例

#### 文本复制功能

```csharp
using UnityEngine;
using FuFramework.OperationClipboard.Runtime;

public class ClipboardManager : MonoBehaviour
{
    [SerializeField] private string m_DefaultText = "默认文本";
    
    // 复制文本到剪贴板
    public void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("要复制的文本不能为空");
            return;
        }
        
        try
        {
            BlankOperationClipboard.SetValue(text);
            Debug.Log($"文本已复制到剪贴板: {text}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"复制到剪贴板失败: {ex.Message}");
        }
    }
    
    // 从剪贴板粘贴文本
    public string PasteFromClipboard()
    {
        try
        {
            string clipboardText = BlankOperationClipboard.GetValue();
            
            if (string.IsNullOrEmpty(clipboardText))
            {
                Debug.Log("剪贴板为空");
                return string.Empty;
            }
            
            Debug.Log($"从剪贴板获取文本: {clipboardText}");
            return clipboardText;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"从剪贴板获取文本失败: {ex.Message}");
            return string.Empty;
        }
    }
    
    // UI 按钮回调方法
    public void OnCopyButtonClicked()
    {
        CopyToClipboard(m_DefaultText);
    }
    
    public void OnPasteButtonClicked()
    {
        string pastedText = PasteFromClipboard();
        if (!string.IsNullOrEmpty(pastedText))
        {
            // 处理粘贴的文本
            ProcessPastedText(pastedText);
        }
    }
    
    private void ProcessPastedText(string text)
    {
        // 在这里处理粘贴的文本
        Debug.Log($"处理粘贴文本: {text}");
    }
}
```

#### 输入框剪贴板集成

```csharp
using UnityEngine;
using UnityEngine.UI;
using FuFramework.OperationClipboard.Runtime;

public class ClipboardInputField : MonoBehaviour
{
    [SerializeField] private InputField m_InputField;
    
    private void Start()
    {
        if (m_InputField == null)
        {
            m_InputField = GetComponent<InputField>();
        }
    }
    
    // 复制输入框内容到剪贴板
    public void CopyInputFieldToClipboard()
    {
        if (m_InputField != null && !string.IsNullOrEmpty(m_InputField.text))
        {
            BlankOperationClipboard.SetValue(m_InputField.text);
            Debug.Log("输入框内容已复制到剪贴板");
        }
    }
    
    // 从剪贴板粘贴到输入框
    public void PasteClipboardToInputField()
    {
        string clipboardText = BlankOperationClipboard.GetValue();
        
        if (!string.IsNullOrEmpty(clipboardText) && m_InputField != null)
        {
            m_InputField.text = clipboardText;
            Debug.Log("剪贴板内容已粘贴到输入框");
        }
    }
    
    // 清空剪贴板
    public void ClearClipboard()
    {
        BlankOperationClipboard.SetValue(string.Empty);
        Debug.Log("剪贴板已清空");
    }
}
```

### 3. 高级用法

#### 剪贴板监控

```csharp
using UnityEngine;
using FuFramework.OperationClipboard.Runtime;

public class ClipboardMonitor : MonoBehaviour
{
    private string m_LastClipboardContent = string.Empty;
    private float m_CheckInterval = 1.0f; // 检查间隔（秒）
    private float m_LastCheckTime = 0f;
    
    private void Update()
    {
        // 定时检查剪贴板变化
        if (Time.time - m_LastCheckTime >= m_CheckInterval)
        {
            CheckClipboardChange();
            m_LastCheckTime = Time.time;
        }
    }
    
    private void CheckClipboardChange()
    {
        string currentContent = BlankOperationClipboard.GetValue();
        
        if (currentContent != m_LastClipboardContent)
        {
            if (string.IsNullOrEmpty(m_LastClipboardContent) && !string.IsNullOrEmpty(currentContent))
            {
                // 剪贴板从空变为有内容
                OnClipboardContentAdded(currentContent);
            }
            else if (!string.IsNullOrEmpty(m_LastClipboardContent) && string.IsNullOrEmpty(currentContent))
            {
                // 剪贴板从有内容变为空
                OnClipboardContentCleared();
            }
            else
            {
                // 剪贴板内容发生变化
                OnClipboardContentChanged(m_LastClipboardContent, currentContent);
            }
            
            m_LastClipboardContent = currentContent;
        }
    }
    
    private void OnClipboardContentAdded(string content)
    {
        Debug.Log($"剪贴板新增内容: {content}");
        // 处理新增内容
    }
    
    private void OnClipboardContentCleared()
    {
        Debug.Log("剪贴板内容被清空");
        // 处理清空操作
    }
    
    private void OnClipboardContentChanged(string oldContent, string newContent)
    {
        Debug.Log($"剪贴板内容变化: {oldContent} -> {newContent}");
        // 处理内容变化
    }
}
```

#### 数据格式转换

```csharp
using UnityEngine;
using System.Text.Json;
using FuFramework.OperationClipboard.Runtime;

public class ClipboardDataManager : MonoBehaviour
{
    // 复制对象到剪贴板（JSON序列化）
    public void CopyObjectToClipboard<T>(T obj)
    {
        try
        {
            string json = JsonSerializer.Serialize(obj);
            BlankOperationClipboard.SetValue(json);
            Debug.Log($"对象已复制到剪贴板: {typeof(T).Name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"对象复制失败: {ex.Message}");
        }
    }
    
    // 从剪贴板粘贴对象（JSON反序列化）
    public T PasteObjectFromClipboard<T>()
    {
        try
        {
            string json = BlankOperationClipboard.GetValue();
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("剪贴板为空");
                return default(T);
            }
            
            T obj = JsonSerializer.Deserialize<T>(json);
            Debug.Log($"对象已从剪贴板粘贴: {typeof(T).Name}");
            return obj;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"对象粘贴失败: {ex.Message}");
            return default(T);
        }
    }
    
    // 示例：复制玩家数据
    [System.Serializable]
    public class PlayerData
    {
        public string PlayerName { get; set; }
        public int Level { get; set; }
        public int Score { get; set; }
    }
    
    public void CopyPlayerData(PlayerData playerData)
    {
        CopyObjectToClipboard(playerData);
    }
    
    public PlayerData PastePlayerData()
    {
        return PasteObjectFromClipboard<PlayerData>();
    }
}
```

## 平台特定说明

### Android 平台

#### 权限要求
在 Android 平台上，剪贴板操作通常不需要特殊权限，但建议在 AndroidManifest.xml 中添加以下权限（如果需要）：

```xml
<!-- 可选：访问剪贴板权限 -->
<uses-permission android:name="android.permission.READ_CLIPBOARD" />
<uses-permission android:name="android.permission.WRITE_CLIPBOARD" />
```

#### 原生实现
模块使用 `com.alianhome.operationclipboard.MainActivity` 类进行剪贴板操作：

```java
// Android 原生实现（示例）
public class MainActivity extends Activity {
    public static String GetClipBoard() {
        ClipboardManager clipboard = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
        if (clipboard.hasPrimaryClip()) {
            ClipData clipData = clipboard.getPrimaryClip();
            if (clipData != null && clipData.getItemCount() > 0) {
                return clipData.getItemAt(0).getText().toString();
            }
        }
        return "";
    }
    
    public static void SetClipBoard(String text) {
        ClipboardManager clipboard = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
        ClipData clipData = ClipData.newPlainText("text", text);
        clipboard.setPrimaryClip(clipData);
    }
}
```

### iOS 平台

#### 原生实现
模块使用 Objective-C 原生代码进行剪贴板操作：

```objective-c
// iOS 原生实现
char * GetClipBoard() {
    UIPasteboard *pasteboard = [UIPasteboard generalPasteboard];
    const char *text = [pasteboard.string UTF8String];
    char *result = (char*)malloc(strlen(text)+1);
    strcpy(result, text);
    return result;
}

void SetClipBoard(char *text) {
    UIPasteboard *pasteboard = [UIPasteboard generalPasteboard];
    pasteboard.string = [NSString stringWithUTF8String:text];
}
```

### PC/Standalone 平台

#### 实现方式
使用 Unity 的 TextEditor 类实现剪贴板操作：

```csharp
// PC 平台实现
var textEditor = new TextEditor { text = text };
textEditor.OnFocus();
textEditor.Copy();
```

### WebGL 平台

#### 浏览器限制
在 WebGL 平台下，剪贴板操作受到浏览器安全策略的限制：

- 需要用户主动触发（如点击事件）
- 某些浏览器可能不支持剪贴板操作
- 建议提供备选方案

## 性能优化建议

### 1. 避免频繁操作

```csharp
// 避免在 Update 中频繁读取剪贴板
private float m_LastClipboardReadTime = 0f;
private const float MIN_READ_INTERVAL = 0.5f; // 最小读取间隔

public string GetClipboardWithThrottle()
{
    if (Time.time - m_LastClipboardReadTime < MIN_READ_INTERVAL)
    {
        return m_CachedClipboardContent;
    }
    
    m_CachedClipboardContent = BlankOperationClipboard.GetValue();
    m_LastClipboardReadTime = Time.time;
    return m_CachedClipboardContent;
}
```

### 2. 批量操作优化

```csharp
// 批量设置多个文本到剪贴板
public void SetMultipleTextsToClipboard(List<string> texts)
{
    if (texts == null || texts.Count == 0) return;
    
    // 合并文本（根据需求选择合适的分隔符）
    string combinedText = string.Join("\n---\n", texts);
    BlankOperationClipboard.SetValue(combinedText);
}
```

### 3. 内存管理

```csharp
// 处理大文本时的内存优化
public void SetLargeTextToClipboard(string largeText)
{
    if (string.IsNullOrEmpty(largeText)) return;
    
    // 限制文本长度，避免内存问题
    const int MAX_TEXT_LENGTH = 10000;
    
    if (largeText.Length > MAX_TEXT_LENGTH)
    {
        Debug.LogWarning($"文本过长，已截断: {largeText.Length} > {MAX_TEXT_LENGTH}");
        largeText = largeText.Substring(0, MAX_TEXT_LENGTH);
    }
    
    BlankOperationClipboard.SetValue(largeText);
}
```

## 注意事项

### 1. 平台兼容性

- **Android**：需要确保原生插件正确集成
- **iOS**：需要确保原生代码正确编译
- **WebGL**：受浏览器安全策略限制
- **PC**：兼容性最好，但不同操作系统可能有差异

### 2. 安全考虑

- 避免在剪贴板中存储敏感信息
- 对用户输入进行适当的验证和清理
- 考虑剪贴板内容的隐私保护

### 3. 用户体验

- 提供明确的剪贴板操作反馈
- 处理剪贴板操作失败的情况
- 考虑提供备选的文本输入方式

### 4. 错误处理

```csharp
// 完整的错误处理示例
public bool SafeSetClipboard(string text)
{
    try
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("要设置的文本为空");
            return false;
        }
        
        BlankOperationClipboard.SetValue(text);
        return true;
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"剪贴板设置失败: {ex.Message}");
        return false;
    }
}

public string SafeGetClipboard()
{
    try
    {
        return BlankOperationClipboard.GetValue();
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"剪贴板获取失败: {ex.Message}");
        return string.Empty;
    }
}
```

## API 参考

### BlankOperationClipboard 静态方法

| 方法 | 说明 | 返回值 | 参数 |
|------|------|--------|------|
| `GetValue()` | 获取剪贴板内容 | `string` | 无 |
| `SetValue(string text)` | 设置剪贴板内容 | `void` | `text`: 要设置的文本 |

## 示例项目

参考 FuFramework 示例项目中的剪贴板操作示例，了解完整的使用场景和最佳实践。

---

**注意：** 本模块是独立模块，不依赖其他 FuFramework 模块，可以直接在项目中使用。