namespace SilkyUIFramework.Layout;

/// <summary>
/// 布局模块
/// </summary>
public abstract class LayoutModule(UIElementGroup container)
{
    public readonly UIElementGroup Container = container;

    /// <summary>
    /// 准备数据, 计算开始前
    /// </summary>
    public virtual void PrepareData() { }

    /// <summary>
    /// 测量完子元素后调用
    /// </summary>
    public virtual void MeasureChildren() { }

    /// <summary>
    /// 测量完自身后调用
    /// </summary>
    public virtual void Measure() { }

    public virtual void ResizeChildrenWidth()
    {
        // 固宽, 更子宽
        if (Container.FitWidth) return;

        var width = Container.InnerBounds.Width;
        foreach (var element in Container.InFlowChildren)
        {
            element.UpdateWidth(width);
        }
    }

    public virtual void RecalculateHeight() { }

    public virtual void RecalculateChildrenHeight() { }

    public virtual void ResizeChildrenHeight()
    {
        // 固高, 更子高
        if (Container.FitHeight) return;

        var height = Container.InnerBounds.Height;
        foreach (var element in Container.InFlowChildren)
        {
            element.UpdateHeight(height);
        }
    }

    public virtual void UpdateChildrenLayoutPosition() { }
}
