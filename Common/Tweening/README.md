# Tweening

`SilkyUIFramework.Common.Tweening` 是一套按时间驱动的补间动画工具。它本身只负责动画编排、属性插值、回调、缓动曲线和生命周期管理；核心代码只使用 .NET 基础类型，不依赖具体 UI 框架。

## 类概览

- `Tween`: 动画编排器。Step 之间顺序执行，同一个 Step 内的条目并行执行。
- `TweenEntry`: 动画条目的基类，提供 `SetEase`、`SetTrans`、`SetDelay`、`SetDuration` 等链式配置。
- `TweenProperty`: 属性插值条目，由 `Tween.TweenProperty<T>` 内部创建，外部不直接实例化。
- `TweenCallback`: 回调条目，延迟到期后执行一次回调并完成。
- `TweenManager`: 可选的管理器，负责创建、注册、更新和回收 `Tween`。
- `TransitionType`: 过渡曲线类型，例如 `Linear`、`Quad`、`Cubic`、`Sine`、`Bounce` 等。
- `EaseType`: 缓动方向，包括 `In`、`Out`、`InOut`、`OutIn`。

## 基本用法

可以直接创建 `Tween`，调用 `Play()` 后在自己的更新循环中传入每帧间隔秒数。

```csharp
var tween = new Tween();

tween.TweenProperty<float>(
        v => obj.X = v,
        () => obj.X,
        100f,
        0.25f,
        (from, to, t) => from + (to - from) * t)
    .SetEase(EaseType.Out)
    .SetTrans(TransitionType.Quad);

tween.Play();

// 每帧调用
tween.Update(deltaSeconds);
```

也可以使用 `TweenManager` 托管多个 Tween。`CreateTween()` 会创建、注册并自动 `Play()`。

```csharp
var tween = TweenManager.Instance.CreateTween();

// 每帧调用
TweenManager.Instance.Update(totalTime);
```

`TweenManager.Update(TimeSpan time)` 会根据本次和上次传入时间计算 `deltaSeconds`。第一次调用只记录时间，不会推进动画；如果传入时间倒退，delta 会被限制为 `0`。

## 属性补间

`TweenProperty<T>` 会在延迟到期时调用一次 `getter` 获取起始值，然后每帧使用 `lerpFunc` 从起始值插值到目标值。属性条目内部保留具体的泛型类型，值类型补间不会在每帧经过 `object` 装箱路径。

```csharp
tween.TweenProperty<float>(
        v => obj.Scale = v,
        () => obj.Scale,
        1.5f,
        0.4f,
        (from, to, t) => from + (to - from) * t)
    .SetEase(EaseType.InOut)
    .SetTrans(TransitionType.Sine);
```

如果 `Duration` 为 `0`，属性会在延迟结束后直接设置为目标值并完成。传入负数时会被限制为 `0`。

## 回调

`TweenCallback` 的 `Duration` 固定为 `0`，只受 `Delay` 影响。

```csharp
tween.TweenCallback(() =>
{
    obj.Visible = false;
}).SetDelay(0.2f);
```

## 顺序与并行

默认是顺序模式：每次添加的条目各自成为一个 Step，按添加顺序执行。

```csharp
tween.TweenProperty<float>(v => obj.X = v, () => obj.X, 100f, 0.2f, (from, to, t) => from + (to - from) * t);
tween.TweenProperty<float>(v => obj.Y = v, () => obj.Y, 50f, 0.2f, (from, to, t) => from + (to - from) * t);
```

调用 `Parallel()` 后，后续条目会加入同一个新 Step，并行执行。调用 `Sequential()` 可切回顺序模式。

```csharp
tween.Parallel();
tween.TweenProperty<float>(v => obj.X = v, () => obj.X, 100f, 0.2f, (from, to, t) => from + (to - from) * t);
tween.TweenProperty<float>(v => obj.Y = v, () => obj.Y, 50f, 0.2f, (from, to, t) => from + (to - from) * t);

tween.Sequential();
tween.TweenCallback(OnMoveFinished);
```

`TweenProperty` 和 `TweenCallback` 返回的是 `TweenEntry`，用于配置这个条目；如果还要继续添加其他条目，请保留并使用 `Tween` 变量。

## 循环与生命周期

```csharp
tween.SetLoops(3);
```

- `SetLoops(1)` 是默认值，播放一次。
- `SetLoops(-1)` 表示无限循环。
- 其他值必须大于 `0`，否则会抛出 `ArgumentOutOfRangeException`。
- 每次循环会重置所有条目；属性补间会在下一轮延迟到期时重新读取 `getter` 作为起始值。

生命周期状态由 `TweenState` 表示：

- `Idle`: 尚未开始。
- `Playing`: 正在播放。
- `Paused`: 已暂停，可调用 `Play()` 恢复。
- `Finished`: 已结束，可能是自然完成或 `Kill()` 终止；进入该状态后不能恢复。

`Kill()` 会触发 `OnFinished`，自然完成时也会通过内部 `Kill()` 触发 `OnFinished`。`TweenManager` 会在后续更新中回收已结束的 `Tween`。

## 注意事项

- `TweenManager.Register(tween)` 只把 `Tween` 放入 pending 列表，下一次 `Update` 才会合并到 active 列表。
- `TransitionType.Linear` 会直接返回线性进度，此时 `EaseType` 不产生额外效果。
- `SetDelay` 和 `SetDuration` 会把负数限制为 `0`。

## 在 SilkyUIFramework 中使用

在本项目的 UI 元素中，推荐使用 `UIView.CreateTween()`。它会创建已注册到全局 `TweenManager` 的 `Tween`，并把 Tween 的有效性绑定到当前 UI 元素生命周期；元素离开 UI 树后，对应 Tween 会被自动终止。

```csharp
var tween = CreateTween();

tween.TweenProperty<float>(
        v => Opacity = v,
        () => Opacity,
        1f,
        0.25f,
        MathHelper.Lerp)
    .SetEase(EaseType.Out)
    .SetTrans(TransitionType.Quad);
```

全局 `TweenManager` 的更新时间由项目接入层负责。本项目中，`UIHookInstaller` 会调用 `TweenManager.Instance.Update(Main.gameTimeCache.TotalGameTime)`。
