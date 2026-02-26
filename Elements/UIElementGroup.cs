using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

/// <summary>
/// 似乎在密谋着什么，再等等...
/// UI 元素容器基类，负责子元素管理、布局分类、更新链路与可选裁剪绘制。
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
    /// 仅在开启 <see cref="OverflowHidden"/> 时有效，通过将内容绘制在新的 RenderTarget 上实现隐藏超出部分的效果
    /// </summary>
    public bool IndependentRenderTarget { get; set; } = true;

    /// <summary> 递归初始化当前容器及其子元素；重复调用是安全的。 </summary>
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
    /// 处理元素进入 UI 树
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
    /// 处理元素退出 UI 树
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
        if (child == null) return;
        if (child.Parent == this) return;

        if (index == null)
        {
            child.RemoveFromParent();
            Elements.Add(child);
            child.Parent = this;
        }
        else if (index >= 0 || index <= Elements.Count)
        {
            child.RemoveFromParent();
            Elements.Insert(index.Value, child);
            child.Parent = this;
        }
        else return;

        MarkLayoutDirty();

        ElementsOrderIsDirty = true;

        OnAddChild(child);
        RuntimeSafeHelper.SafeInvoke(() =>
        {
            if (SilkyUI != null) child.HandleEnterTree(SilkyUI);
        });
        RuntimeSafeHelper.SafeInvoke(child.Initialize);
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

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
        DrawChildren(gameTime, spriteBatch);
    }

    /// <summary>
    /// 计算当前容器用于裁剪的屏幕空间矩形。
    /// </summary>
    /// <remarks>
    /// 返回值会与当前设备的 ScissorRectangle 取交集，避免越界裁剪。
    /// </remarks>
    public virtual Rectangle GetClippingRectangle(SpriteBatch spriteBatch)
    {
        var bounds = HiddenBox switch
        {
            HiddenBox.Outer => OuterBounds,
            HiddenBox.Inner => InnerBounds,
            _ => Bounds,
        };

        var topLeft = Vector2.Transform(bounds.Position, SilkyUI.TransformMatrix);
        var rightBottom = Vector2.Transform(bounds.BottomRight, SilkyUI.TransformMatrix);
        var rectangle = new Rectangle(
            (int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y),
            (int)Math.Ceiling(rightBottom.X - topLeft.X),
            (int)Math.Ceiling(rightBottom.Y - topLeft.Y));

        var device = spriteBatch.GraphicsDevice;
        var viewport = device.Viewport;
        var scissorRectangle = device.ScissorRectangle;
        scissorRectangle.X -= viewport.X;
        scissorRectangle.Y -= viewport.Y;
        return Rectangle.Intersect(rectangle, scissorRectangle);
    }

    /// <summary>
    /// 绘制子元素，按配置启用裁剪与可选独立渲染目标。
    /// </summary>
    public virtual void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (OverflowHidden)
        {
            // 进入裁剪分支前先结束当前批次，后续会切换裁剪状态或渲染目标。
            spriteBatch.End();

            var device = spriteBatch.GraphicsDevice;
            var originalScissor = device.ScissorRectangle;
            var scissorRectangle = GetClippingRectangle(spriteBatch);

            if (IndependentRenderTarget && scissorRectangle.Width > 0 && scissorRectangle.Height > 0)
            {
                // 在独立 RenderTarget 中完成裁剪绘制，再回贴到主目标。
                var renderTargetPool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();
                var renderTarget = renderTargetPool.Rent(scissorRectangle.Width, scissorRectangle.Height);

                RuntimeSafeHelper.SafeInvoke(() =>
                {
                    // 临时替换渲染目标与视口，结束后必须完整恢复图形状态。
                    var binding = device.GetRenderTargets();
                    var viewport = device.Viewport;

                    device.SetRenderTarget(renderTarget);
                    device.Clear(Color.Transparent);

                    device.Viewport = device.Viewport.WithXy(-scissorRectangle.X, -scissorRectangle.Y)
                        .IncreaseSize(scissorRectangle.X, scissorRectangle.Y);
                    device.ScissorRectangle = new Rectangle(0, 0, scissorRectangle.Width, scissorRectangle.Height);

                    spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null,
                        SilkyUI.RasterizerStateForOverflowHidden, null, SilkyUI.TransformMatrix);

                    // 先做一层粗略裁剪：仅绘制与容器 InnerBounds 相交的子元素，减少无效绘制。
                    foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(InnerBounds)))
                    {
                        child.HandleDraw(gameTime, spriteBatch);
                    }

                    spriteBatch.End();

                    device.RestoreRenderTargets(binding);
                    device.Viewport = viewport;
                    device.ScissorRectangle = originalScissor;

                    // 将离屏结果绘制回主目标后，恢复正常批次继续后续绘制流程。
                    DrawRenderTarget(spriteBatch, renderTarget, scissorRectangle.Position);
                    spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null,
                        SilkyUI.RasterizerStateForOverflowHidden, null, SilkyUI.TransformMatrix);
                });

                renderTargetPool.Return(renderTarget);

                return;
            }

            // 不启用独立 RenderTarget 时，直接使用设备裁剪矩形进行绘制。
            device.ScissorRectangle = scissorRectangle;
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                SilkyUI.TransformMatrix);

            foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(InnerBounds)))
            {
                child.HandleDraw(gameTime, spriteBatch);
            }

            spriteBatch.End();

            device.ScissorRectangle = originalScissor;
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                SilkyUI.TransformMatrix);

            return;
        }

        foreach (var child in ElementsInOrder)
        {
            child.HandleDraw(gameTime, spriteBatch);
        }
    }

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
    /// 在 <see cref="MeasureChildren"/> 首行调用，按 <see cref="PositioningExtensions.IsOutOfFlow"/> 对当前帧有效子元素分组。<br/>
    /// 实际用于更新和绘制的元素存于 <see cref="ElementsCache"/>。<br/>
    /// 参与布局流计算的元素存于 <see cref="InFlowChildren"/>。<br/>
    /// 脱离布局流的元素存于 <see cref="OutOfFlowChildren"/>。
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

        // 开启溢出隐藏后, 需要先检查自身是否包含点
        if (OverflowHidden)
        {
            if (!ContainsPoint(mousePosition)) return null;

            foreach (var child in ElementsInOrder.Reverse<UIView>())
            {
                var target = child.GetElementAt(mousePosition);
                if (target != null) return target;
            }

            // 所有子元素都不符合条件, 如果自身不忽略鼠标交互, 则返回自己
            return IgnoreMouseInteraction ? null : this;
        }

        // 没有开启溢出隐藏, 直接检查所有有效子元素
        foreach (var child in ElementsInOrder.Reverse<UIView>())
        {
            var target = child.GetElementAt(mousePosition);
            if (target != null) return target;
        }

        // 忽略鼠标交互
        if (IgnoreMouseInteraction) return null;

        // 元素包含点
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