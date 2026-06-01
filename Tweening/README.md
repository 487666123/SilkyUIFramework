# Tweening 使用说明

`SilkyUIFramework.Tweening` 提供一套按时间驱动的补间动画系统。它由 `TweenManager` 统一更新，`Tween` 负责组织动画步骤，`TweenEntry` 表示每个具体动画条目。

## 类概览

- `TweenManager`: 全局管理器，负责创建、注册、更新和回收 `Tween`。项目中已经在 `UIHookInstaller` 里调用 `TweenManager.Instance.Update(Main.gameTimeCache.TotalGameTime)`。
- `Tween`: 动画编排器。Step 之间顺序执行，同一个 Step 内的条目并行执行。
- `TweenEntry`: 动画条目的基类，提供 `SetEase`、`SetTrans`、`SetDelay`、`SetDuration` 等链式配置。
- `TweenProperty`: 属性插值条目，由 `Tween.TweenProperty<T>` 内部创建，外部不直接实例化。
- `TweenCallback`: 回调条目，延迟到期后执行一次回调并完成。
- `TransitionType`: 过渡曲线类型，例如 `Linear`、`Quad`、`Cubic`、`Sine`、`Bounce` 等。
- `EaseType`: 缓动方向，包括 `In`、`Out`、`InOut`、`OutIn`。

## 推荐创建方式

UI 元素内推荐使用 `UIView.CreateTween()`，它会创建已注册到全局管理器的 `Tween`，并在元素离开 UI 树后自动 `Kill`。

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

非 `UIView` 场景可使用全局管理器：

```csharp
var tween = TweenManager.Instance.CreateTween();
```

`TweenManager.CreateTween()` 会自动调用 `Play()` 并注册到管理器。手动 `new Tween()` 时，需要自行 `Play()` 并用 `Update(deltaSeconds)` 驱动，或调用 `TweenManager.Instance.Register(tween)` 交给管理器更新。

## 属性补间

`TweenProperty<T>` 会在延迟到期时调用一次 `getter` 获取起始值，然后每帧使用 `lerpFunc` 从起始值插值到目标值。

```csharp
var tween = CreateTween();

tween.TweenProperty<Vector2>(
        v => Position = v,
        () => Position,
        new Vector2(100f, 40f),
        0.4f,
        Vector2.Lerp)
    .SetEase(EaseType.InOut)
    .SetTrans(TransitionType.Sine);
```

如果 `Duration <= 0`，属性会在延迟结束后直接设置为目标值并完成。

## 回调

`TweenCallback` 的 `Duration` 固定为 `0`，只受 `Delay` 影响。

```csharp
var tween = CreateTween();

tween.TweenCallback(() =>
{
    Visible = false;
}).SetDelay(0.2f);
```

## 顺序与并行

默认是顺序模式：每次添加的条目各自成为一个 Step，按添加顺序执行。

```csharp
var tween = CreateTween();

tween.TweenProperty<float>(v => X = v, () => X, 100f, 0.2f, MathHelper.Lerp);
tween.TweenProperty<float>(v => Y = v, () => Y, 50f, 0.2f, MathHelper.Lerp);
```

调用 `Parallel()` 后，后续条目会加入同一个新 Step，并行执行。调用 `Sequential()` 可切回顺序模式。

```csharp
var tween = CreateTween();

tween.Parallel();
tween.TweenProperty<float>(v => X = v, () => X, 100f, 0.2f, MathHelper.Lerp);
tween.TweenProperty<float>(v => Y = v, () => Y, 50f, 0.2f, MathHelper.Lerp);

tween.Sequential();
tween.TweenCallback(OnMoveFinished);
```

注意：`TweenProperty` 和 `TweenCallback` 返回的是 `TweenEntry`，用于配置这个条目；如果还要继续添加其他条目，请保留并使用 `Tween` 变量。

## 循环与生命周期

```csharp
var tween = CreateTween();

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

- `TweenManager.Update(TimeSpan time)` 第一次调用只记录时间，不会推进动画；之后根据两次调用之间的时间差计算 `deltaSeconds`。
- `TweenManager.Register(tween)` 只把 `Tween` 放入 pending 列表，下一次 `Update` 才会合并到 active 列表。
- `TransitionType.Linear` 会直接返回线性进度，此时 `EaseType` 不产生额外效果。
- `SetDelay` 和 `SetDuration` 不会限制负数输入；传入负数会按当前实现参与时间计算，建议调用方传入非负值。
