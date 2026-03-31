using SilkyUIFramework.Layout.Flexbox;

namespace SilkyUIFramework.Layout;

public sealed class FlexboxModule(UIElementGroup parent) : LayoutModule(parent)
{
    private readonly FlexboxContext _context = new(parent);
    private IFlexboxLayoutStrategy _strategy = parent.FlexDirection switch
    {
        FlexDirection.Column => ColumnLayoutStrategy.Instance,
        _ => RowLayoutStrategy.Instance
    };

    public override void PrepareData()
    {
        // 策略模式
        _strategy = _context.Parent.FlexDirection switch
        {
            FlexDirection.Column => ColumnLayoutStrategy.Instance,
            _ => RowLayoutStrategy.Instance
        };
    }

    /// <summary>
    /// 计算换行
    /// </summary>
    public sealed override void MeasureChildren()
    {
        _strategy.MeasureChildren(_context);
    }

    public sealed override void Measure()
    {
        _strategy.Measure(_context);
    }

    public sealed override void ResizeChildrenWidth()
    {
        base.ResizeChildrenWidth();
        _strategy.ResizeChildrenWidth(_context);
    }

    public sealed override void RecalculateHeight()
    {
        _strategy.RecalculateHeight(_context);
    }

    public sealed override void RecalculateChildrenHeight()
    {
        _strategy.RecalculateChildrenHeight(_context);
    }

    public sealed override void ResizeChildrenHeight()
    {
        base.ResizeChildrenHeight();
        _strategy.ResizeChildrenHeight(_context);
    }


    public sealed override void UpdateChildrenLayoutPosition()
    {
        _strategy.UpdateChildrenLayoutPosition(_context);
    }

}