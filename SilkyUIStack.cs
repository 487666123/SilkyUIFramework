namespace SilkyUIFramework;

[Service(ServiceLifetime.Transient)]
/// <summary>
/// 管理当前模组注册的全部 <see cref="SilkyUI"/>，并负责排序、置顶与命中测试。
/// </summary>
public class SilkyUIStack
{
    /// <summary>
    /// UI 栈中的原始元素顺序。
    /// </summary>
    private readonly List<SilkyUI> _stackItems = [];

    /// <summary>
    /// 排序后的栈元素缓存，用于当前帧遍历。
    /// </summary>
    private readonly List<SilkyUI> _orderedStackItems = [];

    /// <summary>
    /// 指示当前排序缓存是否需要刷新。
    /// </summary>
    private bool _isOrderDirty;

    /// <summary>
    /// 当前排序结果只读视图。
    /// </summary>
    public IReadOnlyList<SilkyUI> OrderedUIs
    {
        get
        {
            EnsureOrdered();
            return _orderedStackItems;
        }
    }

    /// <summary>
    /// 将一个 UI 压入栈尾。
    /// </summary>
    public void Push(SilkyUI ui)
    {
        _stackItems.Add(ui);
        _isOrderDirty = true;
    }

    /// <summary>
    /// 清空全部 UI，并先断开其 Body 引用。
    /// </summary>
    public void Clear()
    {
        foreach (var ui in _stackItems)
        {
            ui.SetBody(null);
        }

        _stackItems.Clear();
        _orderedStackItems.Clear();
        _isOrderDirty = false;
    }

    /// <summary>
    /// 将指定 UI 提升到栈顶。
    /// </summary>
    public void BringToFront(SilkyUI ui)
    {
        if (_stackItems.Remove(ui))
        {
            _stackItems.Insert(0, ui);
            _isOrderDirty = true;
        }
    }

    /// <summary>
    /// 在需要时按优先级重排 UI（高优先级在前）。
    /// </summary>
    private void EnsureOrdered()
    {
        if (!_isOrderDirty) return;

        _orderedStackItems.Clear();
        _orderedStackItems.AddRange(_stackItems.OrderByDescending(value => value.Priority));
        _isOrderDirty = false;
    }

    /// <summary>
    /// 从前到后查找当前坐标命中的最上层元素。
    /// </summary>
    public UIView HitTest(Vector2 position)
    {
        EnsureOrdered();

        foreach (var ui in _orderedStackItems)
        {
            var target = ui.HitTest(position);
            if (target != null)
            {
                return target;
            }
        }

        return null;
    }

    /// <summary>
    /// 更新全部已排序 UI。
    /// </summary>
    public void Update(GameTime gameTime)
    {
        EnsureOrdered();

        foreach (var ui in _orderedStackItems.Where(ui => ui != null))
        {
            ui.Update(gameTime);
        }
    }
}
