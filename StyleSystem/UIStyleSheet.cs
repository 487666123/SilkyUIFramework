using SilkyUIFramework.Common.Reflection;
using SilkyUIFramework.Common.Tweening;
using SilkyUIFramework.Extensions;

namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 一个 UIView 的状态样式。每次应用时合并最新定义，再直接赋值或创建属性过渡。
/// Normal 始终参与合并；需要退出状态后恢复的属性，应在 Normal 中提供基础值。
/// 路径在每次应用时解析，动画固定绑定当时的对象，不追踪运行期间的对象替换。
/// </summary>
/// <param name="element">此样式表唯一绑定的元素；样式定义可以共享，运行状态不能共享。</param>
public class UIStyleSheet(UIView element)
{
    /// <summary>一条属性路径的访问器与当前动画，不保存历史目标或类型。</summary>
    private sealed class PropertyState(PropertyPathAccessor accessor)
    {
        /// <summary>缓存路径解析能力；每次应用样式时重新查找实际对象，动画运行时不再解析路径。</summary>
        public PropertyPathAccessor Accessor { get; } = accessor;

        /// <summary>当前管理的动画；没有动画或动画结束后为 null。</summary>
        public Tween Tween;

        /// <summary>停止当前动画，保留访问器和属性当前值。</summary>
        public void StopTween()
        {
            Tween?.Kill();
            Tween = null;
        }
    }

    // 从低到高覆盖同名属性，和枚举的数值顺序无关。
    private static readonly UIElementState[] StatePriority =
    [
        UIElementState.Normal,
        UIElementState.Hover,
        UIElementState.Focus,
        UIElementState.Selected,
        UIElementState.Active,
        UIElementState.Custom1,
        UIElementState.Custom2,
        UIElementState.Disabled,
    ];

    private readonly UIView _element = element ?? throw new ArgumentNullException(nameof(element));

    // 保存定义本身的引用，因此共享定义的修改能在下次合并时直接读到。
    private readonly Dictionary<UIElementState, StyleDefinition> _styles = [];

    // 按完整属性路径配置过渡；存在专属配置时，即使它不可播放，也不回退到 AllTransition。
    private readonly Dictionary<string, StyleTransition> _transitions = [with(StringComparer.Ordinal)];

    // 每条路径只保存访问器和运行状态，不建立编译结果或状态组合缓存。
    // 路径格式无效时缓存 null，避免每次应用都重新解析并重复警告。
    private readonly Dictionary<string, PropertyState> _properties = [with(StringComparer.Ordinal)];

    // 复用的合并结果字典，避免每次 ApplyStyle 时分配新字典。
    private readonly Dictionary<string, StyleValue> _resolvedValues = new(StringComparer.Ordinal);

    /// <summary>没有属性专属配置时使用的过渡；null 或不可播放的配置表示直接赋值。</summary>
    public StyleTransition AllTransition { get; set; } = new();

    #region set/get style

    /// <summary>
    /// 为指定状态设置样式；多个标志表示分别设置同一份定义，Normal 需单独设置。
    /// 定义可以共享，修改后在各元素下一次 ApplyStyle 时生效。
    /// </summary>
    /// <param name="state">一个或多个状态标志；组合表示批量设置，不是仅在这些状态同时出现时匹配。</param>
    /// <param name="style">保存属性路径与目标值的定义，不能为 null。</param>
    /// <returns>当前样式表，支持链式配置。</returns>
    public UIStyleSheet SetStyle(UIElementState state, StyleDefinition style)
    {
        ArgumentNullException.ThrowIfNull(style);
        foreach (var singleState in StatePriority)
        {
            // Normal 为零，HasFlag(Normal) 总为 true，批量设置时需要显式排除它。
            if (singleState == UIElementState.Normal && state != UIElementState.Normal) continue;
            if (state.HasFlag(singleState)) _styles[singleState] = style;
        }
        return this;
    }

    /// <summary>获取一个已定义单状态的样式；不存在时返回 null。</summary>
    public StyleDefinition GetStyle(UIElementState state)
    {
        ValidateSingleState(state);
        return _styles.GetValueOrDefault(state);
    }

    /// <summary>检查一个已定义单状态是否配置了样式。</summary>
    public bool HasStyle(UIElementState state)
    {
        ValidateSingleState(state);
        return _styles.ContainsKey(state);
    }

    /// <summary>移除单状态的样式，下次应用时生效。</summary>
    public bool RemoveStyle(UIElementState state)
    {
        ValidateSingleState(state);
        return _styles.Remove(state);
    }

    /// <summary>返回配置过样式的状态，不代表元素当前激活的状态；返回值是定义字典的键集合。</summary>
    public IEnumerable<UIElementState> GetDefinedStates() => _styles.Keys;

    /// <summary>给多个属性设置同一份专属过渡；保存配置引用，在下次应用样式时读取。</summary>
    /// <param name="propertyPaths">完整属性路径，区分大小写，每条路径都不能为空白。</param>
    /// <param name="transition">过渡配置；不能为 null，不可播放的配置表示直接赋值。</param>
    /// <returns>当前样式表，支持链式配置。</returns>
    public UIStyleSheet SetTransition(string[] propertyPaths, StyleTransition transition)
    {
        ArgumentNullException.ThrowIfNull(propertyPaths);
        ArgumentNullException.ThrowIfNull(transition);
        foreach (var path in propertyPaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            _transitions[path] = transition;
        }
        return this;
    }

    /// <summary>移除专属过渡，下次应用时使用 AllTransition。</summary>
    public bool RemoveTransition(string propertyPath) => _transitions.Remove(propertyPath);

    #endregion

    /// <summary>
    /// 有目标时先停止旧动画；样式值类型必须与成员声明类型一致，值不同时才应用目标。
    /// 本次未声明目标的属性不作处理，保留运行记录并让已有动画继续。
    /// </summary>
    /// <param name="currentState">此次用于合并样式的状态组合；本方法不修改 UIView.State。</param>
    public void ApplyStyle(UIElementState currentState)
    {
        // 只处理合并结果中明确声明的目标；未出现的路径不停止动画，也不清空记录。
        var values = ResolveValues(currentState);
        foreach (var (path, targetValue) in values)
            ApplyProperty(path, targetValue);
    }

    /// <summary>
    /// 立即停止动画，保留样式定义、路径访问器和元素当前值。
    /// 元素离树时调用；下次应用仍按当时的实际值和目标值判断是否过渡。
    /// </summary>
    public void Release()
    {
        foreach (var property in _properties.Values) property?.StopTween();
    }

    /// <summary>查询和移除只接受 Normal 或一个已定义标志，不接受状态组合。</summary>
    private static void ValidateSingleState(UIElementState state)
    {
        if (!StatePriority.Contains(state))
            throw new ArgumentOutOfRangeException(nameof(state), state, "只允许传入一个已定义的 UI 状态。");
    }

    /// <summary>
    /// 合并基础样式及当前激活状态的样式，高优先级覆盖同路径的低优先级值。
    /// 每次都读取最新定义，复用实例字典以减少 GC 压力。
    /// </summary>
    /// <returns>
    /// 复用的内部字典实例，仅在当前调用的上下文中有效；
    /// 不可保存返回值引用，下次调用会清空并重新填充此字典。
    /// </returns>
    private Dictionary<string, StyleValue> ResolveValues(UIElementState currentState)
    {
        _resolvedValues.Clear();

        foreach (var state in StatePriority)
        {
            if (state != UIElementState.Normal && !currentState.HasFlag(state)) continue;
            if (!_styles.TryGetValue(state, out var style)) continue;
            foreach (var (path, value) in style) _resolvedValues[path] = value;
        }

        return _resolvedValues;
    }

    /// <summary>取得或创建路径运行记录；路径格式无效时返回 null。</summary>
    private PropertyState GetProperty(string path)
    {
        if (_properties.TryGetValue(path, out var property)) return property;

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            property = new PropertyState(new PropertyPathAccessor(_element.GetType(), path.Split('.')));
        }
        catch (ArgumentException exception)
        {
            // 这里只处理路径格式错误；成员不存在或不可访问的错误仍由实际读写抛出。
            Console.WriteLine($"[StyleWarning] {_element.GetType().Name}: 无法解析属性路径 '{path}': {exception.Message}");
        }

        // 失败的格式也缓存下来，避免后续应用重复警告。
        _properties[path] = property;
        return property;
    }

    /// <summary>
    /// 停止旧动画，解析本次实际对象并比较当前值与目标；类型不符时抛出异常，值相等时跳过。
    /// 需要过渡时固定绑定该对象的成员，没有可用过渡时直接赋值。
    /// </summary>
    private void ApplyProperty(string path, StyleValue targetValue)
    {
        var property = GetProperty(path);
        if (property == null) return;
        property.StopTween();

        if (!property.Accessor.TryResolveMember(_element, out var owner, out var memberName, out _)) return;
        var accessor = ObjectAccessorCache.GetAccessor(owner);
        if (targetValue.IsCurrentValue(accessor, owner, memberName)) return;

        var transition = _transitions.GetValueOrDefault(path, AllTransition);
        if (!_element.IsInsideTree || !(transition?.CanPlay() ?? false) || !targetValue.CanTween)
        {
            targetValue.SetValue(accessor, owner, memberName);
            return;
        }

        var tween = property.Tween = _element.CreateTween();
        tween.OnFinished += () =>
        {
            // 防止旧动画完成时清空新动画的引用
            if (ReferenceEquals(property.Tween, tween)) property.Tween = null;
        };

        try
        {
            targetValue.CreateTween(tween, accessor, owner, memberName, transition.Duration)
                .SetEase(transition.Ease)
                .SetTrans(transition.Trans)
                .SetDelay(transition.Delay);
        }
        catch
        {
            tween.Kill();
            throw;
        }
    }
}
