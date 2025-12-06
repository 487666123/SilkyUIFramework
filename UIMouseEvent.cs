namespace SilkyUIFramework;

public class UIMouseEvent(UIView source, Vector2 position)
{
    /// <summary> 最初触发事件的元素 </summary>
    public UIView Source { get; } = source;

    /// <summary> 上一个触发事件的元素 </summary>
    public UIView Previous { get; set; } = source;

    /// <summary> 鼠标位置 </summary>
    public Vector2 MousePosition { get; } = position;
}

public class UIScrollWheelEvent(UIView source, Vector2 position, int scrollDelta) : UIMouseEvent(source, position)
{
    /// <summary> 滚动 </summary>
    public int ScrollDelta { get; } = scrollDelta;

    /// <summary> 应用滚动的元素 </summary>
    public UIView ScrollingElement { get; set; }
}