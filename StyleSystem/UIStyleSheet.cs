using SilkyUIFramework.Common.Reflection;
using SilkyUIFramework.Common.Tweening;
using SilkyUIFramework.Extensions;

namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 一个 UIView 的样式规则表。每次应用时合并最新定义，再直接赋值或创建属性过渡。
/// 无条件规则始终参与合并；需要退出标记后恢复的属性，应在无条件规则中提供基础值。
/// 路径在每次应用时解析，动画固定绑定当时的对象，不追踪运行期间的对象替换。
/// </summary>
/// <param name="element">此样式表唯一绑定的元素；样式定义可以共享，运行状态不能共享。</param>
public class UIStyleSheet(UIView element)
{
    /// <summary>一条属性路径的访问器与当前动画及其绑定信息。</summary>
    private sealed class PropertyState(PropertyPathAccessor accessor)
    {
        /// <summary>缓存路径解析能力；每次应用样式时重新查找实际对象，动画运行时不再解析路径。</summary>
        public PropertyPathAccessor Accessor { get; } = accessor;

        /// <summary>当前管理的动画；没有动画或动画结束后为 null。</summary>
        public Tween Tween;

        public object Owner;
        public StyleValue TargetValue;
        // 保存配置值的快照，以识别同一配置对象被原地修改的情况。
        public (float Duration, float Delay, EaseType Ease, TransitionType Trans) Transition;

        public void ClearTween()
        {
            Tween = null;
            Owner = null;
            TargetValue = null;
            Transition = default;
        }

        /// <summary>停止当前动画，保留访问器和属性当前值。</summary>
        public void StopTween()
        {
            Tween?.Kill();
            ClearTween();
        }
    }

    private readonly UIView _element = element ?? throw new ArgumentNullException(nameof(element));

    private sealed record StyleRule(StyleSelector Selector, StyleDefinition Definition, long Order);

    // 保存定义本身的引用，因此共享定义的修改能在下次合并时直接读到。
    private readonly List<StyleRule> _rules = [];
    private long _nextRuleOrder;

    // 按完整属性路径配置过渡；存在专属配置时，即使它不可播放，也不回退到 AllTransition。
    private readonly Dictionary<string, StyleTransition> _transitions = [with(StringComparer.Ordinal)];

    // 每条路径只保存访问器和运行状态，不建立编译结果或状态组合缓存。
    // 路径格式无效时缓存 null，避免每次应用都重新解析并重复警告。
    private readonly Dictionary<string, PropertyState> _properties = [with(StringComparer.Ordinal)];

    /// <summary>没有属性专属配置时使用的过渡；null 或不可播放的配置表示直接赋值。</summary>
    public StyleTransition DefaultTransition { get; set; } = new();

    #region set/get style

    /// <summary>
    /// 设置基础样式。基础样式始终参与合并。
    /// </summary>
    public UIStyleSheet SetStyle(StyleDefinition style) => SetStyle(StyleSelector.Empty, style);

    /// 设置要求指定标记存在的样式。
    public UIStyleSheet SetStyle(StyleMarker marker, StyleDefinition style) =>
        SetStyle(StyleSelector.AllOf(marker), style);

    /// 设置要求所有指定标记存在的复合样式。
    public UIStyleSheet SetStyle(StyleSelector selector, StyleDefinition style)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(style);

        _rules.RemoveAll(rule => rule.Selector.Equals(selector));
        _rules.Add(new StyleRule(selector, style, _nextRuleOrder++));
        return this;
    }

    /// 设置要求所有指定标记存在的复合样式。
    public UIStyleSheet SetStyle(StyleDefinition style, params StyleMarker[] requiredMarkers) =>
        SetStyle(new StyleSelector(requiredMarkers), style);

    /// <summary>获取一个已定义单标记的样式；不存在时返回 null。</summary>
    public StyleDefinition GetStyle(StyleMarker marker) => GetStyle(StyleSelector.AllOf(marker));

    /// <summary>获取一个已定义选择器的样式；不存在时返回 null。</summary>
    public StyleDefinition GetStyle(StyleSelector selector) =>
        _rules.LastOrDefault(rule => rule.Selector.Equals(selector))?.Definition;

    public bool HasStyle(StyleSelector selector) => GetStyle(selector) is not null;

    public bool HasStyle(StyleMarker marker) => HasStyle(StyleSelector.AllOf(marker));

    public bool RemoveStyle(StyleSelector selector) => _rules.RemoveAll(rule => rule.Selector.Equals(selector)) > 0;

    public bool RemoveStyle(StyleMarker marker) => RemoveStyle(StyleSelector.AllOf(marker));

    public IEnumerable<StyleSelector> GetDefinedSelectors() => _rules.Select(rule => rule.Selector);

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
    /// 目标、绑定对象及过渡配置不变时保留当前动画；否则按当前值重新应用目标。
    /// 样式值类型必须与成员声明类型一致。
    /// 本次未声明目标的属性不作处理，保留运行记录并让已有动画继续。
    /// </summary>
    public void ApplyStyle(IEnumerable<StyleMarker> activeMarkers)
    {
        ArgumentNullException.ThrowIfNull(activeMarkers);
        var values = ResolveValues(activeMarkers);
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

    /// <summary>
    /// 合并所有匹配规则。条件数量越多的规则优先级越高；相同条件数量按注册顺序覆盖。
    /// </summary>
    /// <returns>本次应用解析出的属性目标集合。</returns>
    private Dictionary<string, StyleValue> ResolveValues(IEnumerable<StyleMarker> activeMarkers)
    {
        var activeMarkerSet = activeMarkers.ToHashSet();
        var resolvedValues = new Dictionary<string, StyleValue>();

        foreach (var rule in _rules
                     .Where(rule => rule.Selector.Matches(activeMarkerSet))
                     .OrderBy(rule => rule.Selector.Specificity)
                     .ThenBy(rule => rule.Order))
        {
            foreach (var (path, value) in rule.Definition) resolvedValues[path] = value;
        }

        return resolvedValues;
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
    /// 解析本次实际对象，保留目标与配置未变的动画；否则停止旧动画并比较当前值与目标。
    /// 类型不符时抛出异常，值相等时跳过。
    /// 需要过渡时固定绑定该对象的成员，没有可用过渡时直接赋值。
    /// </summary>
    private void ApplyProperty(string path, StyleValue targetValue)
    {
        var property = GetProperty(path);
        if (property == null) return;
        if (!property.Accessor.TryResolveMember(_element, out var owner, out var memberName, out _))
        {
            property.StopTween();
            return;
        }

        var transition = _transitions.GetValueOrDefault(path, DefaultTransition);
        var canTween = _element.IsInsideTree && (transition?.CanPlay() ?? false) && targetValue.CanTween;
        if (canTween && property.Tween is { IsFinished: false } &&
            ReferenceEquals(property.Owner, owner) && targetValue.HasSameValue(property.TargetValue) &&
            property.Transition == (transition.Duration, transition.Delay, transition.Ease, transition.Trans))
            return;

        property.StopTween();
        var accessor = ObjectAccessorCache.GetAccessor(owner);
        if (targetValue.IsCurrentValue(accessor, owner, memberName)) return;

        if (!canTween)
        {
            targetValue.SetValue(accessor, owner, memberName);
            return;
        }

        var tween = property.Tween = _element.CreateTween();
        property.Owner = owner;
        property.TargetValue = targetValue;
        property.Transition = (transition.Duration, transition.Delay, transition.Ease, transition.Trans);
        tween.OnFinished += () =>
        {
            // 防止旧动画完成时清空新动画的引用
            if (ReferenceEquals(property.Tween, tween)) property.ClearTween();
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
