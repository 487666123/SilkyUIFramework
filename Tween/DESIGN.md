# Godot 风格 Tween — 纯模块设计

## Context

在已有 `Ease.cs` 和 `Transition.cs`（纯数学，无外部依赖）基础上，构建 Godot 风格的 Tween 编排层。Tween 模块**零框架依赖**，只引用同目录下的 Ease 和 Transition。用户自行决定何时调用 `Update(delta)`、如何管理生命周期。

## 文件结构

```
Tween/
├── Ease.cs              (已有 - 不改)
├── Transition.cs        (已有 - 不改)
├── TweenState.cs        新建 - 状态枚举
├── TweenEntry.cs        新建 - 条目抽象基类 + ApplyEasing
├── TweenProperty.cs     新建 - 属性插值 (internal)
├── TweenCallback.cs     新建 - 回调条目
└── Tween.cs             新建 - 编排器主类
```

**不修改框架任何文件**。不引入 DI、不依赖 UIView/SilkyUI。

## 核心设计

### 1. 缓动组合

放在 `TweenEntry` 的静态方法中，组合 `Transition.Map` + `EaseType`：

```
In    → curve(t)
Out   → 1 − curve(1 − t)
InOut → t<0.5 ? curve(2t)*0.5 : 1−curve(2−2t)*0.5
OutIn → t<0.5 ? (1−curve(1−2t))*0.5 : 0.5+curve(2t−1)*0.5
```

### 2. 条目基类 (`TweenEntry.cs`)

```csharp
public abstract class TweenEntry
{
    public float Duration { get; set; }
    public float Delay { get; set; }
    public EaseType EaseType { get; set; } = EaseType.InOut;
    public TransitionType TransitionType { get; set; } = TransitionType.Linear;

    public TweenEntry SetEase(EaseType t);
    public TweenEntry SetTrans(TransitionType t);
    public TweenEntry SetDelay(float d);
    public TweenEntry SetDuration(float d);

    internal float Elapsed;
    internal bool IsCompleted;
    internal abstract void Tick(float delta);
    internal virtual void Reset();

    internal static float ApplyEasing(float t, TransitionType trans, EaseType ease);
}
```

### 3. 属性插值 (`TweenProperty.cs`, internal)

`lerpFunc` **必传**，不做类型预设。构造时装箱一次，Tick 零分配。

```csharp
internal class TweenProperty : TweenEntry
{
    // 存 object from/to, Action<object> setter, Func<object,object,float,object> lerp
    internal static TweenProperty Create<T>(
        Action<T> setter, T from, T to, float duration,
        Func<T, T, float, T> lerpFunc);

    internal override void Tick(float delta)
    {
        Elapsed += delta;
        if (Elapsed < Delay) return;
        float rawT = Math.Clamp((Elapsed - Delay) / Duration, 0, 1);
        _setter(_lerpFunc(_from, _to, ApplyEasing(rawT, TransitionType, EaseType)));
        if (rawT >= 1f) { _setter(_to); IsCompleted = true; }
    }
}
```

### 4. 回调条目 (`TweenCallback.cs`)

```csharp
public class TweenCallback : TweenEntry
{
    public TweenCallback(Action callback) { Duration = 0f; ... }
    internal override void Tick(float delta) { ... } // Delay 后触发一次即完成
}
```

### 5. 编排器 (`Tween.cs`)

核心结构：`List<TweenStep>`，Step 间顺序执行，Step 内并行执行。

```csharp
public class Tween : IDisposable
{
    // 配置
    public Tween SetLoops(int count);   // 1=默认, -1=无限
    public Tween Parallel();            // 后续条目加入当前 Step
    public Tween Sequential();          // 后续条目加入新 Step

    // 添加条目（返回 TweenEntry 供 fluent）
    public TweenEntry TweenProperty<T>(Action<T> setter, T from, T to, float duration,
        Func<T, T, float, T> lerpFunc);
    public TweenEntry TweenCallback(Action callback);

    // 生命周期
    public void Play();
    public void Pause();
    public void Resume();
    public void Stop();
    public void Reset();

    // 状态
    public TweenState State { get; }
    public bool IsPlaying { get; }
    public float TotalElapsed { get; }

    // 事件
    public event Action? OnCompleted;
    public event Action? OnStopped;

    // 每帧由用户调用
    public void Update(float deltaSeconds);

    // 释放事件引用
    public void Dispose();
}
```

状态机：`Idle → Playing → (Paused ⇄ Playing) → Completed/Stopped`

### 6. 使用示例

```csharp
var tween = new Tween();

tween.TweenProperty<Color>(v => element.BackgroundColor = v,
        Color.Transparent, Color.White, 0.5f, Color.Lerp)
     .SetEase(EaseType.Out).SetTrans(TransitionType.Quad);

tween.TweenCallback(() => Console.WriteLine("fade done"));

tween.Parallel();
tween.TweenProperty<float>(v => element.Border = v, 0f, 4f, 0.2f,
        (a, b, t) => a + (b - a) * t);
tween.TweenProperty<Vector2>(v => element.LayoutOffset = v,
        Vector2.Zero, new(10, 0), 0.2f, Vector2.Lerp);

tween.SetLoops(-1);
tween.Play();

// 用户在自己的更新循环中调用：
tween.Update(deltaSeconds);
```

## 修改的文件

| 文件 | 改动 |
|---|---|
| `Tween/TweenState.cs` | 新建 - 枚举，~5 行 |
| `Tween/TweenEntry.cs` | 新建 - 抽象基类 + ApplyEasing |
| `Tween/TweenProperty.cs` | 新建 - 属性插值，internal |
| `Tween/TweenCallback.cs` | 新建 - 回调条目 |
| `Tween/Tween.cs` | 新建 - 编排器（最大文件） |

**不修改框架任何文件。**

## 内存与性能

- 构造时：每个 TweenProperty 装箱 from/to 一次，分配 List 预设 4
- Tick 热路径：零分配，缓存委托调用，数组索引查 Transition.Map
- `Dispose()` 清空事件引用，Tween 无引用后正常 GC

## 验证方式

`dotnet build` 通过即可，功能由用户自行验证。
