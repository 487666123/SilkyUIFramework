namespace SilkyUIFramework.Elements;

public abstract class UIDragControl : UIElementGroup
{
    private Vector2 _mousePositionAtPress;

    protected abstract UIView DragThumb { get; }

    protected abstract void SetValueAtMousePosition();

    protected abstract void CaptureValueAtPress();

    protected abstract void UpdateValueByDrag(Vector2 offset, Vector2 availableSpace);

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);

        if (evt.Source != DragThumb)
            SetValueAtMousePosition();

        CaptureValueAtPress();
        _mousePositionAtPress = evt.MousePosition;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        if (!LeftMousePressed)
            return;

        var availableSpace = InnerBounds.Size - DragThumb.Bounds.Size;
        var offset = Main.MouseScreen - _mousePositionAtPress;

        UpdateValueByDrag(offset, availableSpace);
    }

    /// <summary>
    /// 根据当前鼠标位置获取归一化值。
    /// </summary>
    public Vector2 GetValueAtMousePosition()
    {
        var start = InnerBounds.Position + DragThumb.Bounds.Size / 2f;
        var availableSpace = InnerBounds.Size - DragThumb.Bounds.Size;

        return (Main.MouseScreen - start) / availableSpace;
    }
}
