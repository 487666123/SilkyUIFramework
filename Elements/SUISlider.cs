using SilkyUIFramework.Animation;

namespace SilkyUIFramework.Elements;

/// <summary>
/// 滑块轨道进度条
/// </summary>
public class SUISliderTrackProgress : UIView
{
    public SUISliderTrackProgress()
    {
        Positioning = Positioning.Absolute;
        Width = new Dimension(0f, 0f);
        Height = new Dimension(0f, 1f);

        BackgroundColor = Color.LightGreen;
        //BackgroundColor = new Color(0x33, 0xCC, 0x55);
    }
}


/// <summary>
/// 滑块轨道
/// </summary>
public class SUISliderTrack : UIElementGroup
{
    public SUISliderTrackProgress ProgressBar { get; }

    public SUISliderTrack()
    {
        Positioning = Positioning.Absolute;
        SetLeft(0f, 0f, 0.5f);
        SetTop(0f, 0f, 0.5f);
        Height = new Dimension(0f, 0.5f);

        Border = 2f;
        BorderColor = Color.White;
        BackgroundColor = Color.LightCoral;
        //BackgroundColor = new Color(0xDD, 0x55, 0x33);

        ProgressBar = new SUISliderTrackProgress().Join(this);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        var minSize = Math.Min(Bounds.Width, Bounds.Height);

        Width = new Dimension(-(minSize), 1f);

        BorderRadius = new(minSize / 2f);
        ProgressBar.BorderRadius = new(Math.Min(ProgressBar.Bounds.Width, ProgressBar.Bounds.Height) / 2f);
    }
}


/// <summary>
/// 滑块
/// </summary>
public class SUISliderThumb : UIView
{
    private readonly struct ThumbAnimationArgs(float border) : IInterpolable<ThumbAnimationArgs>
    {
        public float Border { get; } = border;

        public readonly ThumbAnimationArgs Lerp(ThumbAnimationArgs target, float t) => new(MathHelper.Lerp(Border, target.Border, t));

        public bool Equals(ThumbAnimationArgs other) => Border == other.Border;
    }

    private readonly AutoAnimation<ThumbAnimationArgs> _animation = new();

    public SUISliderThumb()
    {
        Positioning = Positioning.Absolute;
        SetLeft(0f, 0f, 0f);
        SetTop(0f, 0f, 0.5f);

        SetSize(0f, 0f, 0f, 1f);

        BorderColor = Color.White;
        BackgroundColor = SUIColor.Background;

        _animation.OnChanged += (_, value) => Border = value.Border;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        if (Parent.LeftMousePressed) _animation.Value = new(6f);
        else if (IsMouseHovering) _animation.Value = new(2f);
        else _animation.Value = new(4f);

        _animation.Update(gameTime);

        base.UpdateStatus(gameTime);

        BorderRadius = new(Math.Min(Bounds.Width, Bounds.Height) / 2f - 0.75f);

        SetWidth(Bounds.Height);
    }
}

/// <summary>
/// 滑块条
/// </summary>
[XmlElementMapping("Slider")]
public class SUISlider : UIElementGroup
{
    public SUISliderThumb Thumb { get; }
    public SUISliderTrack Track { get; }

    public event EventHandler<float> ValueChanged;

    public float Value
    {
        get => field;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (field == clamped) return;
            field = clamped;
            OnValueChanged(field);
        }
    }

    public float Step
    {
        get; set
        {
            if (field == value) return;
            field = value;
            OnDrag(Value);
        }
    }

    /// <summary>
    /// 2025.12.4 记：在整理调用顺序的设计时得到更清晰的结论。
    /// <para/>
    /// 子类对 OnValueChanged 的重写应视为对父类行为的补充，而不是替代。
    /// <br/>
    /// 父类负责将控件的内部状态更新到稳定状态，子类则在此基础上扩展逻辑。
    /// <br/>
    /// 事件订阅者的行为模式与此一致，都是基于已更新的状态继续执行。
    /// <para/>
    /// 因此，将核心更新逻辑置于父类方法中，并在触发事件前完成，是更一致且可维护的设计方式。
    /// </summary>
    protected virtual void OnValueChanged(float value)
    {
        Thumb?.SetLeft(0f, 0f, value);
        Track?.ProgressBar.SetWidth(null, value);

        ValueChanged?.Invoke(this, value);
    }

    public event EventHandler<float> Drag;

    protected virtual void OnDrag(float value)
    {
        if (Step > 0)
            value = SnapByStep(value, Step);
        Value = value;
        Drag?.Invoke(this, Value);
    }

    public static float SnapByStep(float value, float step) => MathF.Round(value / step) * step;

    public SUISlider()
    {
        Width = new Dimension(0f, 1f);
        Height = new Dimension(0f, 1f);

        Track = new SUISliderTrack().Join(this);
        Thumb = new SUISliderThumb().Join(this);
    }

    private Vector2 _mousePositionAtPress;
    private float _valuetAtPress;

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);

        // 支持直接点条确定位置
        if (evt.Source != Thumb)
        {
            Value = GetValueAtMousePosition().X;
        }

        // 记录按下时的状态
        _valuetAtPress = Value;
        _mousePositionAtPress = evt.MousePosition;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        if (LeftMousePressed)
        {
            var space = InnerBounds.Size - Thumb.Bounds.Size;
            var offset = Main.MouseScreen - _mousePositionAtPress;

            var value = _valuetAtPress + offset.X / space.Width;

            OnDrag(value);
        }
    }

    /// <summary>
    /// 根据当前鼠标位置获取 Value 理论值
    /// </summary>
    public Vector2 GetValueAtMousePosition()
    {
        var start = InnerBounds.Position + Thumb.Bounds.Size / 2f;
        var space = InnerBounds.Size - Thumb.Bounds.Size;

        return (Main.MouseScreen - start) / space;
    }
}