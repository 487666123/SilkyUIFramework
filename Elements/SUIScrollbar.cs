using Newtonsoft.Json.Linq;
using System.Windows.Input;

namespace SilkyUIFramework.Elements;

public class SUIScrollbarThumb : UIView
{
    public (Color Default, Color Hover) BarColor { get; set; } = (Color.Black * 0.2f, Color.Black * 0.3f);

    public bool IsActive { get; set; }

    public SUIScrollbarThumb()
    {
        Positioning = Positioning.Absolute;
        BorderRadius = new Vector4(2f);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        BackgroundColor = IsActive || LeftMousePressed || IsMouseHovering
            ? BarColor.Hover
            : BarColor.Default;
    }
}

public class SUIScrollbar : UIDragControl
{
    public readonly SUIScrollbarThumb Thumb = new();

    protected override UIView DragThumb => Thumb;

    public event EventHandler<Vector2> Drag;

    protected virtual void OnDrag(Vector2 value)
    {
        Value = value;
        Drag?.Invoke(this, Value);
    }

    public Vector2 ViewportRatio
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Thumb.SetSize(0, 0, value.X, value.Y);
        }
    } = Vector2.One;

    /// <summary> 同步滑块显示位置，不触发滚动请求。 </summary>
    public Vector2 Value
    {
        get;
        set
        {
            var progress = Vector2.Clamp(value, Vector2.Zero, Vector2.One);
            if (float.IsNaN(progress.X)) progress.X = 0;
            if (float.IsNaN(progress.Y)) progress.Y = 0;

            if (field == progress) return;
            field = progress;

            Thumb.SetLeft(0f, 0f, progress.X);
            Thumb.SetTop(0f, 0f, progress.Y);
        }
    }

    public SUIScrollbar()
    {
        Border = 0;
        Thumb.Join(this);
    }

    private Vector2 _valueAtPress;

    protected override void SetValueAtMousePosition()
    {
        Value = GetValueAtMousePosition();
    }

    protected override void CaptureValueAtPress()
    {
        _valueAtPress = Value;
    }

    protected override void UpdateValueByDrag(Vector2 offset, Vector2 availableSpace)
    {
        OnDrag(_valueAtPress + offset / availableSpace);
    }
}
