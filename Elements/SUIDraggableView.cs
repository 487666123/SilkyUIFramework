namespace SilkyUIFramework.Elements;

/// <summary>
/// 可拖动视图
/// </summary>
[XmlElementMapping("DraggableView")]
public class SUIDraggableView : UIElementGroup
{
    public bool Dragging { get; protected set; }
    public Vector2 DragIncrement { get; set; } = Vector2.Zero;
    public Vector2 MouseDragOffset { get; set; } = Vector2.Zero;
    public bool OccupyMouseInterface { get; set; } = true;
    public bool ConstrainInParent { get; set; } = false;

    public UIElementGroup ControlTarget { get; set; }

    public SUIDraggableView()
    {
        BackgroundColor = Color.Black * 0.25f;
    }

    public SUIDraggableView(UIElementGroup controlTarget) : base()
    {
        ControlTarget = controlTarget;
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);

        if (ControlTarget == null || evt.Source != this) return;

        Dragging = true;
        MouseDragOffset = evt.MousePosition - ControlTarget.DragOffset;
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        base.OnLeftMouseUp(evt);
        Dragging = false;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        if (!Dragging) return;

        var x = Main.mouseX - MouseDragOffset.X;
        var y = Main.mouseY - MouseDragOffset.Y;

        if (DragIncrement.X != 0) x -= x % DragIncrement.X;
        if (DragIncrement.Y != 0) y -= y % DragIncrement.Y;

        // 计算新的拖拽偏移
        var newDragOffset = new Vector2(x, y);

        // 如果需要限制在父元素内
        if (ConstrainInParent && ControlTarget.Parent != null)
        {
            // 获取父元素的内部边界
            var parentBounds = ControlTarget.Parent.InnerBounds;
            // 获取控制目标的外部边界（不包含拖拽偏移）
            var targetBounds = ControlTarget.OuterBounds;
            var originalX = targetBounds.X - ControlTarget.DragOffset.X;
            var originalY = targetBounds.Y - ControlTarget.DragOffset.Y;

            // 计算允许的拖拽范围
            var minX = parentBounds.X - originalX;
            var minY = parentBounds.Y - originalY;
            var maxX = parentBounds.Right - originalX - targetBounds.Width;
            var maxY = parentBounds.Bottom - originalY - targetBounds.Height;

            // 限制拖拽偏移在允许范围内
            newDragOffset.X = MathHelper.Clamp(newDragOffset.X, minX, maxX);
            newDragOffset.Y = MathHelper.Clamp(newDragOffset.Y, minY, maxY);
        }

        ControlTarget.DragOffset = newDragOffset;
    }
}