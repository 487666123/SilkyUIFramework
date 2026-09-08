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

public class SUIScrollbar : UIElementGroup
{
    public readonly SUIScrollbarThumb Thumb = new();

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

    public Vector2 GetValueAtMousePosition()
    {
        var start = InnerBounds.Position + Thumb.Bounds.Size / 2f;
        var space = InnerBounds.Size - Thumb.Bounds.Size;

        return (Main.MouseScreen - start) / space;
    }

    private Vector2 _mousePositionAtPress;
    private Vector2 _valuetAtPress;

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);

        // 支持直接点条确定位置
        if (evt.Source != Thumb)
            Value = GetValueAtMousePosition();

        // 记录按下时的状态
        _valuetAtPress = Value;
        _mousePositionAtPress = evt.MousePosition;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        if (LeftMousePressed)
        {
            var space = (Vector2)(InnerBounds.Size - Thumb.Bounds.Size);
            var offset = Main.MouseScreen - _mousePositionAtPress;

            OnDrag(_valuetAtPress + offset / space);
        }
    }
}
