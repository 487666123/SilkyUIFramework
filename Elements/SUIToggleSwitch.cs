using SilkyUIFramework.Animation;
using SilkyUIFramework.Components;

namespace SilkyUIFramework.Elements;

/// <summary>
/// 拨动开关
/// </summary>
[XmlElementMapping("ToggleSwitch")]
public class SUIToggleSwitch : UIView
{
    public SUIToggleSwitch()
    {
        SetPadding(2f);
        SetSize(36f, 20f);

        Border = 2f;
        BorderRadius = new Vector4(10f);
        BorderColor = SUIColor.Border * 0.75f;

        InternalRectangleRender.BackgroundColor = SUIColor.Background * 0.75f;
    }

    /// <summary> 状态改变时触发 </summary>
    public event Action<bool> OnStatusChanges;

    private bool _status;

    /// <summary> 状态 </summary>
    public virtual bool Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            StatusChanged(value);
        }
    }

    public readonly RectangleRender InternalRectangleRender = new();
    public readonly AnimationTimer SwitchTimer = new(3);

    public virtual void StatusChanged(bool value)
    {
        OnStatusChanges?.Invoke(value);
        if (value) SwitchTimer.StartUpdate();
        else SwitchTimer.StartReverseUpdate();
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        Status = !Status;
        base.OnLeftMouseDown(evt);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        SwitchTimer.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        var position = InnerBounds.Position;
        var size = InnerBounds.Size;
        var beadSize = new Vector2(MathHelper.Min(size.Width, size.Height));
        var end = InnerBounds.BottomRight - beadSize;

        InternalRectangleRender.BorderRadius = new Vector4(beadSize.Y / 2f);
        InternalRectangleRender.Draw(SwitchTimer.Lerp(position, end), beadSize, false,ref SilkyUI.TransformMatrix);
    }
}