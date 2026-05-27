# Logger (旧)
> **源文件**: `Assets/Scripts/Utility/Logger.cs`

已标记为"待废弃"的旧 Logger。具备智能序列化能力，目前作为 ConsoleAppender 的格式化后端保留。

## 调用链

```
被谁调:
  ConsoleAppender.Append()        → Logger.Log() / LogWarning() / LogError()
  (旧代码中残留的直接调用)         → Logger.Log() / LogWarning() / LogError()

调谁:
  Log() / LogWarning() / LogError() → Debug.Log() / LogWarning() / LogError()
  BuildMessage()                     → Serialize() → 递归序列化
  Serialize()                       → SerializeDictionary / SerializeEnumerable
                                       / SerializeUsingJsonUtility / SerializeWithReflection
  FormatSimple()                     → 格式化浮点/向量/颜色等
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | ConsoleAppender | ConsoleAppender 使用 Logger 做格式化输出 |
| 被依赖 | (旧代码) | 历史遗留的直接调用 |
| 依赖 | MetaStruct (01-core) | StructWithMeta 泛型结构体使用 MetaStruct |
| 依赖 | System.Reflection | SerializeWithReflection 通过反射读取字段 |
| 依赖 | UnityEngine.JsonUtility | SerializeUsingJsonUtility 尝试 JSON 序列化 |

## 方法

### Log()
```csharp
public static void Log(object payload, string tag = null, UnityEngine.Object context = null, bool prettyPrint = false)
```
- **用途**: 输出 Info 级别日志
- **参数**:
  - `payload` — 要记录的对象（将被序列化为字符串）
  - `tag` — 可选标签，覆盖自动生成的类型标签
  - `context` — 可选 Unity Object 上下文（点击日志可高亮对象）
  - `prettyPrint` — 是否格式化换行缩进
- **调用者**: ConsoleAppender（Trace/Debug/Info 级别）
- **备注**: 最终调用 `Debug.Log()`

### LogWarning()
```csharp
public static void LogWarning(object payload, string tag = null, UnityEngine.Object context = null, bool prettyPrint = false)
```
- **用途**: 输出 Warning 级别日志
- **参数**: 同 Log()
- **调用者**: ConsoleAppender（Warning 级别）
- **备注**: 最终调用 `Debug.LogWarning()`

### LogError()
```csharp
public static void LogError(object payload, string tag = null, UnityEngine.Object context = null, bool prettyPrint = false)
```
- **用途**: 输出 Error 级别日志
- **参数**: 同 Log()
- **调用者**: ConsoleAppender（Error/Fatal 级别）
- **备注**: 最终调用 `Debug.LogError()`

### BuildMessage()
```csharp
private static string BuildMessage(LogLevel level, string tagOverride, object payload, bool prettyPrint)
```
- **用途**: 构建格式化日志字符串 `[Level][Tag] serialized_content`
- **参数**:
  - `level` — 内部 LogLevel 枚举（Info/Warning/Error）
  - `tagOverride` — 覆盖标签（null 时自动取 payload.GetType().Name）
  - `payload` — 日志内容
  - `prettyPrint` — 是否格式化换行缩进
- **返回**: 格式化后的完整日志字符串
- **调用者**: Log() / LogWarning() / LogError()
- **备注**: StringBuilder 初始容量 256 以减少内存分配

### Serialize()
```csharp
private static string Serialize(object payload, bool prettyPrint, int depth, HashSet<object> visited)
```
- **用途**: 递归序列化对象为可读字符串
- **参数**:
  - `payload` — 待序列化对象
  - `prettyPrint` — 是否格式化
  - `depth` — 当前递归深度（上限 4）
  - `visited` — 引用类型去重集合（循环引用检测）
- **返回**: 序列化字符串
- **逻辑分支**:
  1. null → "null"
  2. 深度 > 4 → `<MaxDepthReached>`
  3. 简单类型 (IsSimple) → `FormatSimple()`
  4. Unity Object → "name (TypeName)"
  5. 引用类型 → visited 检测循环引用
  6. IDictionary → `SerializeDictionary()`
  7. IEnumerable → `SerializeEnumerable()`
  8. JsonUtility 尝试 → `SerializeUsingJsonUtility()`
  9. 反射 → `SerializeWithReflection()`

### SerializeDictionary()
```csharp
private static string SerializeDictionary(IDictionary dictionary, bool prettyPrint, int depth, HashSet<object> visited)
```
- **用途**: 序列化字典为 `{ key: value, ... }` 格式
- **支持**: prettyPrint 模式多行缩进，单行模式紧凑输出

### SerializeEnumerable()
```csharp
private static string SerializeEnumerable(IEnumerable enumerable, bool prettyPrint, int depth, HashSet<object> visited)
```
- **用途**: 序列化可枚举集合为 `[elem1, elem2, ...]` 格式
- **支持**: prettyPrint 模式多行缩进

### SerializeUsingJsonUtility()
```csharp
private static string SerializeUsingJsonUtility(object payload, bool prettyPrint)
```
- **用途**: 尝试用 JsonUtility.ToJson 序列化
- **返回**: JSON 字符串，或空字符串（失败时）
- **备注**: 包裹 try-catch，反射和序列化失败时不抛异常

### SerializeWithReflection()
```csharp
private static string SerializeWithReflection(object payload, bool prettyPrint, int depth, HashSet<object> visited)
```
- **用途**: 通过反射读取所有实例字段 (public + nonpublic) 序列化
- **返回**: `{ fieldName: value, ... }` 格式
- **降级**: 无公开字段时调用 `payload.ToString()`

### IsSimple()
```csharp
private static bool IsSimple(Type type)
```
- **用途**: 判断类型是否为可直接格式化的"简单类型"
- **返回**: true 表示该类型由 FormatSimple 直接处理
- **匹配类型**: 原始类型 / 枚举 / string / decimal / double / float / Vector2/3/4 / Quaternion / Color / Vector2Int / Vector3Int

### FormatSimple()
```csharp
private static string FormatSimple(object value)
```
- **用途**: 格式化简单类型值为字符串
- **特殊处理**:
  - float/double/decimal → "F3" 三位小数
  - Vector2 → `(x, y)`
  - Vector3 → `(x, y, z)`
  - Vector4 → `(x, y, z, w)`
  - Quaternion → `(x, y, z, w)`
  - Color → `RGBA(r, g, b, a)` 两位小数
  - 其他 → `ToString()`

## 内部类型

### LogLevel (私有枚举)
```csharp
private enum LogLevel { Info, Warning, Error }
```
- 与 `LogLevel.cs` 定义的公共枚举不同，此枚举只有三级
- ConsoleAppender 调用时做级别映射:
  - Trace/Debug/Info → Logger.Log（对应 Info 级别）
  - Warning → Logger.LogWarning
  - Error/Fatal → Logger.LogError

### StructWithMeta<TStruct>
```csharp
private readonly struct StructWithMeta<TStruct> where TStruct : struct
{
    public StructWithMeta(TStruct payload, MetaStruct meta)
    public TStruct Payload { get; }
    public MetaStruct Meta { get; }
}
```
- 泛型结构体包装器，将运行时值与 MetaStruct 绑定
- 当前未在公开 API 中使用，属于框架预留扩展点

### ReferenceEqualityComparer
```csharp
private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
```
- 循环引用检测专用，使用引用相等性而非值相等性
- 通过 `RuntimeHelpers.GetHashCode` 获取对象标识哈希

## 设计说明

- **深度限制 (4 层)**: 防止递归序列化导致调用栈溢出
- **引用跟踪**: 使用 ReferenceEqualityComparer 的 HashSet，同一个引用类型对象只序列化一次
- **降级路径**: Unity 对象 → JsonUtility → 反射 → ToString()，层层降级保证总能输出
- **StringBuilder 初始容量**: 256 字符减少高频分配

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 废弃此 Logger，功能迁至 Logging 系统 | 待做 | 架构规划 + 代码注释 |
| ConsoleAppender 直接格式化，不依赖旧 Logger | 待做 | 架构规划 |
