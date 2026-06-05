using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

/// <summary>
/// UI 元素容器基类。
/// 负责子元素管理、布局分组、更新链路与可选裁剪绘制。
/// </summary>
[XmlElementMapping("ElementGroup")]
public partial class UIElementGroup : UIView
{
    /// <summary>
    /// 创建一个元素容器，并初始化默认布局模块。
    /// </summary>
    public UIElementGroup()
    {
        FlexboxModule = new FlexboxModule(this);
        GridModule = new GridModule(this);
    }

    /// <summary>
    /// 是否启用内容裁剪，启用后仅绘制容器可视范围内的子元素区域。
    /// </summary>
    public bool OverflowHidden { get; set; }

    /// <summary>
    /// 仅在开启 <see cref="OverflowHidden"/> 时生效。
    /// 通过在独立 RenderTarget 中绘制并回贴，隐藏超出容器可视范围的内容。
    /// </summary>
    public bool IndependentRenderTarget { get; set; } = true;

    /// <summary>
    /// 递归初始化当前容器及其子元素。
    /// 重复调用是安全的。
    /// </summary>
    internal sealed override void Initialize()
    {
        base.Initialize();

        foreach (var item in Elements)
        {
            item.Initialize();
        }
    }

    /// <summary>
    /// 所有直接子元素（包含当前帧可能不会参与更新与绘制的元素）。
    /// </summary>
    protected List<UIView> Elements { get; } = [];

    /// <summary>
    /// 当前帧有效子元素缓存（已过滤 <see cref="UIView.Invalid"/>），用于布局、更新和绘制。
    /// </summary>
    protected List<UIView> ElementsCache { get; } = [];

    /// <summary>
    /// 直接子元素只读视图。
    /// </summary>
    public IReadOnlyList<UIView> Children => Elements;

    /// <summary>
    /// <see cref="ElementsCache"/> 的只读视图。
    /// </summary>
    public IReadOnlyList<UIView> ChildrenCache => ElementsCache;

    /// <summary>
    /// 返回子元素在 <see cref="Children"/> 中的索引，不存在则返回 -1。
    /// </summary>
    public int IndexOf(UIView view) => Elements.IndexOf(view);

    /// <summary>
    /// 返回子元素在 <see cref="ChildrenCache"/> 中的索引，不存在则返回 -1。
    /// </summary>
    public int IndexOfInCache(UIView view) => ElementsCache.IndexOf(view);

    /// <summary>
    /// 处理当前容器及其直接子元素进入 UI 树。
    /// </summary>
    internal sealed override void HandleEnterTree(SilkyUI silkyUI)
    {
        if (SilkyUI != null || silkyUI == null) return;
        base.HandleEnterTree(silkyUI);

        foreach (var el in Elements)
        {
            el.HandleEnterTree(silkyUI);
        }
    }

    /// <summary>
    /// 处理当前容器及其直接子元素退出 UI 树。
    /// </summary>
    internal sealed override void HandleExitTree()
    {
        if (SilkyUI == null) return;
        base.HandleExitTree();

        foreach (var el in Elements)
        {
            el.HandleExitTree();
        }
    }

    /// <summary>
    /// 清理当前容器脏标记，并递归清理 <see cref="InFlowElements"/>。
    /// <see cref="OutOfFlowElements"/> 由 <c>MarkFreeElementsDirty</c> 按需标记。
    /// </summary>
    public override void CleanupDirtyMark()
    {
        base.CleanupDirtyMark();

        foreach (var child in InFlowElements)
        {
            child.CleanupDirtyMark();
        }

        MarkFreeElementsDirty();
    }

    #region Append Remove RemoveChild

    /// <summary>
    /// 判断是否包含指定直接子元素。
    /// </summary>
    public virtual bool HasChild(UIView child) => Elements.Contains(child);

    /// <summary>
    /// 添加子元素到当前容器，可选插入位置。
    /// </summary>
    /// <param name="child">要添加的子元素。</param>
    /// <param name="index">插入索引；为 <see langword="null"/> 时追加到末尾。</param>
    public void AddChild(UIView child, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(child);

        for (var current = this; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, child))
                throw new InvalidOperationException("Cannot add an ancestor node as a child.");
        }

        if (index == null)
        {
            child.RemoveFromParent();
            Elements.Add(child);
            child.Parent = this;
        }
        else if (index >= 0 && index <= Elements.Count)
        {
            child.RemoveFromParent();
            Elements.Insert(index.Value, child);
            child.Parent = this;
        }
        else return;

        MarkLayoutDirty();

        child.UpdateDataContext();

        ElementsOrderIsDirty = true;

        OnAddChild(child);
        child.HandleEnterTree(SilkyUI);
        child.Initialize();
    }

    /// <summary>
    /// 子元素添加后的扩展钩子。
    /// </summary>
    /// <param name="child">已加入容器的子元素。</param>
    protected virtual void OnAddChild(UIView child) { }

    /// <summary>
    /// 从当前容器移除指定直接子元素。
    /// </summary>
    /// <param name="child">要移除的子元素。</param>
    public void RemoveChild(UIView child)
    {
        if (!Elements.Remove(child)) return;

        child.Parent = null;
        child.UpdateDataContext();
        MarkLayoutDirty();
        ElementsOrderIsDirty = true;

        OnRemoveChild(child);
        child.HandleExitTree();
    }

    /// <summary>
    /// 子元素移除后的扩展钩子。
    /// </summary>
    /// <param name="child">已从容器移除的子元素。</param>
    protected virtual void OnRemoveChild(UIView child) { }

    internal sealed override void UpdateDataContext()
    {
        base.UpdateDataContext();

        foreach (var element in Elements)
        {
            if (element.LocalDataContext == null)
                element.UpdateDataContext();
        }
    }

    /// <summary>
    /// 移除当前容器的全部直接子元素。
    /// </summary>
    public void RemoveAllChildren()
    {
        foreach (var child in Elements.ToArray())
        {
            RemoveChild(child);
        }
    }

    #endregion

    #region Update UpdateStatus Draw

    /// <summary>
    /// 更新当前容器，并向下分发子元素更新。
    /// </summary>
    public override void HandleUpdate(GameTime gameTime)
    {
        base.HandleUpdate(gameTime);
        UpdateChildren(gameTime);
    }

    /// <summary>
    /// 更新子元素逻辑。
    /// </summary>
    protected virtual void UpdateChildren(GameTime gameTime)
    {
        foreach (var child in ElementsCache) child.HandleUpdate(gameTime);
    }

    /// <summary>
    /// 更新当前容器状态，并向下分发子元素状态更新。
    /// </summary>
    public override void HandleUpdateStatus(GameTime gameTime)
    {
        base.HandleUpdateStatus(gameTime);
        UpdateChildrenStatus(gameTime);
    }

    /// <summary>
    /// 更新子元素交互状态与过渡状态。
    /// </summary>
    protected virtual void UpdateChildrenStatus(GameTime gameTime)
    {
        foreach (var child in ElementsCache)
        {
            child.HandleUpdateStatus(gameTime);
        }
    }

    /// <summary>
    /// 绘制当前容器，并按当前配置绘制子元素。
    /// </summary>
    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
        DrawChildren(gameTime, spriteBatch);
    }

    public Bounds GetVisibleArea() => HiddenBox switch
    {
        HiddenBox.Outer => OuterBounds,
        HiddenBox.Inner => InnerBounds,
        _ => Bounds,
    };

    /// <summary>
    /// 计算当前容器用于裁剪的屏幕空间矩形。
    /// </summary>
    /// <remarks>
    /// 返回值会与当前设备的 ScissorRectangle 取交集，避免越界裁剪。
    /// </remarks>
    public virtual Rectangle GetClippingRectangle(GraphicsDevice device)
    {
        var rectangle = TransformBoundsToClipping(GetVisibleArea(), SilkyUI.TransformMatrix);

        var viewport = device.Viewport;
        var scissorRectangle = device.ScissorRectangle;
        scissorRectangle.X -= viewport.X;
        scissorRectangle.Y -= viewport.Y;
        return Rectangle.Intersect(rectangle, scissorRectangle);
    }

    /// <summary>
    /// 将布局空间中的 Bounds 按变换矩阵转换为屏幕空间的裁剪矩形。
    /// </summary>
    /// <param name="bounds">布局空间中的边界矩形。</param>
    /// <param name="transformMatrix">坐标变换矩阵。</param>
    /// <returns>屏幕空间的整数裁剪矩形，各项已使用 Floor/Ceiling 外扩以防止漏裁。</returns>
    public static Rectangle TransformBoundsToClipping(Bounds bounds, Matrix transformMatrix)
    {
        var topLeft = Vector2.Transform(bounds.Position, transformMatrix);
        var rightBottom = Vector2.Transform(bounds.BottomRight, transformMatrix);
        return new Rectangle(
            (int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y),
            (int)Math.Ceiling(rightBottom.X - topLeft.X),
            (int)Math.Ceiling(rightBottom.Y - topLeft.Y));
    }

    /// <summary>
    /// 绘制子元素，按配置启用裁剪与可选独立渲染目标。
    /// </summary>
    public virtual void DrawChildren(GameTime gameTime, SpriteBatch sb)
    {
        if (!OverflowHidden)
        {
            foreach (var child in ElementsInOrder)
            {
                child.HandleDraw(gameTime, sb);
            }
            return;
        }

        // 进入裁剪分支前先结束当前批次，后续会切换裁剪状态或渲染目标。
        sb.End();

        var device = sb.GraphicsDevice;
        var originalScissor = device.ScissorRectangle;
        var scissorRectangle = GetClippingRectangle(sb.GraphicsDevice);

        if (IndependentRenderTarget && scissorRectangle.Width > 0 && scissorRectangle.Height > 0)
        {
            // 在独立 RenderTarget 中完成裁剪绘制，再回贴到主目标。
            var rtPool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();

            var rectangle = TransformBoundsToClipping(GetVisibleArea(), SilkyUI.TransformMatrix);
            var renderTarget = rtPool.Rent(rectangle.Width, rectangle.Height);

            var bindings = device.GetRenderTargets();
            var viewport = device.Viewport;

            try
            {
                device.SetRenderTarget(renderTarget);
                device.Clear(Color.Transparent);

                device.Viewport = device.Viewport.WithXy(-scissorRectangle.X, -scissorRectangle.Y)
                    .WithSize(scissorRectangle.Right, scissorRectangle.Bottom);

                device.ScissorRectangle = new Rectangle(0, 0, scissorRectangle.Width, scissorRectangle.Height);

                sb.Begin(SpriteSortMode.Deferred, null, null, null,
                    SilkyUI.ScissorRasterizerState, null, SilkyUI.TransformMatrix);

                // 先做一层粗略裁剪：仅绘制与容器 InnerBounds 相交的子元素，减少无效绘制。
                foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(InnerBounds)))
                {
                    child.HandleDraw(gameTime, sb);
                }

                sb.End();
            }
            finally
            {
                try
                {
                    device.RestoreRenderTargets(bindings);
                    device.Viewport = viewport;
                    device.ScissorRectangle = originalScissor;

                    // 将离屏结果绘制回主目标后，恢复正常批次继续后续绘制流程。
                    DrawRenderTarget(sb, renderTarget, scissorRectangle.Position);
                    sb.Begin(SpriteSortMode.Deferred, null, null, null,
                        SilkyUI.ScissorRasterizerState, null, SilkyUI.TransformMatrix);
                }
                finally
                {
                    rtPool.Return(renderTarget);
                }
            }

            return;
        }

        // 不启用独立 RenderTarget 时，直接使用设备裁剪矩形进行绘制。
        device.ScissorRectangle = scissorRectangle;
        sb.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.ScissorRasterizerState, null,
            SilkyUI.TransformMatrix);

        foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(InnerBounds)))
        {
            child.HandleDraw(gameTime, sb);
        }

        sb.End();

        device.ScissorRectangle = originalScissor;
        sb.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.ScissorRasterizerState, null,
            SilkyUI.TransformMatrix);
    }

    /// <summary>
    /// 将独立 RenderTarget 的内容绘制回主目标。
    /// </summary>
    /// <param name="spriteBatch">当前绘制批次。</param>
    /// <param name="renderTarget">待回贴的离屏纹理。</param>
    /// <param name="position">回贴到主目标时的左上角位置。</param>
    protected virtual void DrawRenderTarget(SpriteBatch spriteBatch, RenderTarget2D renderTarget, Vector2 position)
    {
        var scale = Main.UIScale;
        spriteBatch.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        SDFRectangle.SampleVersion(renderTarget, position, renderTarget.SizeVec2,
            Vector2.Zero, Vector2.One, (BorderRadius - new Vector4(2)) * scale, Color.White, Matrix.Identity);
    }

    #endregion

    /// <summary>
    /// 不参与布局流计算的子元素集合。
    /// </summary>
    protected readonly List<UIView> OutOfFlowElements = [];

    /// <summary>
    /// 参与布局流计算的子元素集合。
    /// </summary>
    protected readonly List<UIView> InFlowElements = [];

    /// <summary>
    /// 当前帧脱离布局流子元素只读视图。
    /// </summary>
    public IReadOnlyList<UIView> OutOfFlowChildren => OutOfFlowElements;

    /// <summary>
    /// 当前帧参与布局子元素只读视图。
    /// </summary>
    public IReadOnlyList<UIView> InFlowChildren => InFlowElements;

    /// <summary>
    /// 建议在 <see cref="MeasureChildren"/> 开始处调用，重建当前帧子元素分类缓存。
    /// 有效元素写入 <see cref="ElementsCache"/>，再按 <see cref="PositioningExtensions.IsOutOfFlow"/>
    /// 分入 <see cref="InFlowElements"/> 与 <see cref="OutOfFlowElements"/>。
    /// </summary>
    protected virtual void ClassifyChildren()
    {
        ElementsCache.Clear();
        ElementsCache.AddRange(Elements.Where(el => !el.Invalid));
        OutOfFlowElements.Clear();
        InFlowElements.Clear();

        foreach (var child in ElementsCache)
        {
            if (child.Positioning.IsOutOfFlow)
            {
                OutOfFlowElements.Add(child);
            }
            else
            {
                InFlowElements.Add(child);
            }
        }
    }

    /// <summary>
    /// 命中测试：从上层绘制顺序向下查找鼠标位置对应元素。
    /// </summary>
    public override UIView GetElementAt(Vector2 mousePosition)
    {
        if (DisableMouseInteraction) return null;

        // 开启溢出隐藏后，先检查鼠标点是否位于当前容器可命中区域。
        if (OverflowHidden)
        {
            if (!ContainsPoint(mousePosition)) return null;

            foreach (var child in ElementsInOrder.Reverse<UIView>())
            {
                var target = child.GetElementAt(mousePosition);
                if (target != null) return target;
            }

            // 子元素均未命中时，若当前元素可交互则返回自身。
            return IgnoreMouseInteraction ? null : this;
        }

        // 未开启溢出隐藏时，直接按绘制顺序逆序检测子元素命中。
        foreach (var child in ElementsInOrder.Reverse<UIView>())
        {
            var target = child.GetElementAt(mousePosition);
            if (target != null) return target;
        }

        // 当前元素忽略鼠标交互时直接返回 null。
        if (IgnoreMouseInteraction) return null;

        // 子元素未命中时，命中当前元素则返回自身。
        return ContainsPoint(mousePosition) ? this : null;
    }

    /// <summary>
    /// 容器滚动偏移量；更新时会标记位置脏状态。
    /// </summary>
    public Vector2 ScrollOffset
    {
        get;
        protected set
        {
            if (field == value) return;
            field = value;
            MarkPositionDirty();
        }
    }
}
