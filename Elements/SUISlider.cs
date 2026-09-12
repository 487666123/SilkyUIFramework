using System.Windows.Input;
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

    public Orientation Orientation { get; set; } = Orientation.Horizontal;

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

        if (Orientation == Orientation.Horizontal)
            Width = new Dimension(-minSize, 1f);
        else
            Height = new Dimension(-minSize, 1f);

        BorderRadius = new(minSize / 2f);
        ProgressBar.BorderRadius = new(Math.Min(ProgressBar.Bounds.Width, ProgressBar.Bounds.Height) / 2f);
    }
}


/// <summary>
/// 滑块
/// </summary>
public class SUISliderThumb : UIView
{
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

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

        if (Orientation == Orientation.Horizontal)
            SetWidth(Bounds.Height);
        else
            SetHeight(Bounds.Width);
    }
}

/// <summary>
/// 滑块条
/// </summary>
[XmlElementMapping("Slider")]
public class SUISlider : UIDragControl
{
    public SUISliderThumb Thumb { get; }
    public SUISliderTrack Track { get; }

    protected override UIView DragThumb => Thumb;

    public Orientation Orientation
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            ConfigureOrientationLayout();
        }
    } = Orientation.Horizontal;

    public event EventHandler<Vector2> ValueChanged;

    public Vector2 Value
    {
        get => field;
        set
        {
            var normalized = NormalizeValue(value);
            if (field == normalized) return;
            field = normalized;
            OnValueChanged(field);
        }
    }

    public Vector2 Step
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
    protected virtual void OnValueChanged(Vector2 value)
    {
        if (Orientation == Orientation.Horizontal)
        {
            Thumb?.SetLeft(0f, 0f, value.X);
            Track?.ProgressBar.SetSize(0f, 0f, value.X, 1f);
        }
        else
        {
            Thumb?.SetTop(0f, 0f, value.Y);
            Track?.ProgressBar.SetSize(0f, 0f, 1f, value.Y);
        }

        ValueChanged?.Invoke(this, value);
    }

    public event EventHandler<Vector2> Drag;

    public ICommand DragCommand { get; set; }

    protected virtual void OnDrag(Vector2 value)
    {
        value = SnapByStep(value, Step);
        Value = value;

        if (DragCommand != null && DragCommand.CanExecute(Value))
            DragCommand.Execute(Value);

        Drag?.Invoke(this, Value);
    }

    public static Vector2 SnapByStep(Vector2 value, Vector2 step)
    {
        if (step.X > 0f)
            value.X = MathF.Round(value.X / step.X) * step.X;
        if (step.Y > 0f)
            value.Y = MathF.Round(value.Y / step.Y) * step.Y;
        return value;
    }

    public SUISlider(Orientation orientation = Orientation.Horizontal)
    {
        Width = new Dimension(0f, 1f);
        Height = new Dimension(0f, 1f);

        Track = new SUISliderTrack().Join(this);
        Thumb = new SUISliderThumb().Join(this);

        Orientation = orientation;
        ConfigureOrientationLayout();
    }

    private Vector2 _valueAtPress;

    protected override void SetValueAtMousePosition()
    {
        var value = GetValueAtMousePosition();
        Value = Orientation == Orientation.Horizontal
            ? new Vector2(value.X, 0f)
            : new Vector2(0f, value.Y);
    }

    protected override void CaptureValueAtPress()
    {
        _valueAtPress = Value;
    }

    protected override void UpdateValueByDrag(Vector2 offset, Vector2 availableSpace)
    {
        var value = _valueAtPress;
        if (Orientation == Orientation.Horizontal)
            value.X += offset.X / availableSpace.X;
        else
            value.Y += offset.Y / availableSpace.Y;

        OnDrag(value);
    }

    private Vector2 NormalizeValue(Vector2 value) => Orientation == Orientation.Horizontal
        ? new Vector2(Math.Clamp(value.X, 0f, 1f), 0f)
        : new Vector2(0f, Math.Clamp(value.Y, 0f, 1f));

    private void ConfigureOrientationLayout()
    {
        if (Track is null || Thumb is null)
            return;

        Track.Orientation = Orientation;
        Thumb.Orientation = Orientation;

        if (Orientation == Orientation.Horizontal)
        {
            Track.SetLeft(0f, 0f, 0.5f);
            Track.SetTop(0f, 0f, 0.5f);
            Track.Width = new Dimension(0f, 1f);
            Track.Height = new Dimension(0f, 0.5f);
            Track.ProgressBar.SetSize(0f, 0f, Value.X, 1f);

            Thumb.SetLeft(0f, 0f, Value.X);
            Thumb.SetTop(0f, 0f, 0.5f);
            Thumb.SetSize(0f, 0f, 0f, 1f);
        }
        else
        {
            Track.SetLeft(0f, 0f, 0.5f);
            Track.SetTop(0f, 0f, 0.5f);
            Track.Width = new Dimension(0f, 0.5f);
            Track.Height = new Dimension(0f, 1f);
            Track.ProgressBar.SetSize(0f, 0f, 1f, Value.Y);

            Thumb.SetLeft(0f, 0f, 0.5f);
            Thumb.SetTop(0f, 0f, Value.Y);
            Thumb.SetSize(0f, 0f, 1f, 0f);
        }
    }
}