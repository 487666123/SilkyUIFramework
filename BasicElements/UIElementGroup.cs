using System;
using SilkyUIFramework.Helper;

namespace SilkyUIFramework;

/// <summary>
/// 似乎在密谋着什么，再等等...
/// </summary>
[XmlElementMapping("ElementGroup")]
public partial class UIElementGroup : UIView
{
    public bool OverflowHidden { get; set; }

    /// <summary> 需要持续调用，以保证所有元素都被初始化 </summary>
    internal sealed override void Initialize()
    {
        base.Initialize();

        foreach (var item in Elements)
        {
            item.Initialize();
        }
    }

    protected List<UIView> Elements { get; } = [];
    protected List<UIView> ElementsCache { get; } = [];
    public IReadOnlyList<UIView> Children => Elements;
    public IReadOnlyList<UIView> ChildrenCache => ElementsCache;

    /// <summary>
    /// 处理元素进入 UI 树
    /// </summary>
    internal sealed override void HandleEnterTree(SilkyUI silkyUI)
    {
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
        base.HandleExitTree();

        foreach (var el in Elements)
        {
            el.HandleExitTree();
        }
    }

    /// <summary>
    /// 尺寸与布局完成后, 清理脏标记 (只会清理 <see cref="LayoutChildren"/>)
    /// </summary>
    public override void CleanupDirtyMark()
    {
        base.CleanupDirtyMark();

        foreach (var child in LayoutChildren)
        {
            child.CleanupDirtyMark();
        }
    }

    #region Append Remove RemoveChild

    public virtual bool HasChild(UIView child) => Elements.Contains(child);

    public virtual void Add(UIView child, int? index = null) => AppendChild(child, index);

    public virtual void Add(IEnumerable<UIView> children, int? index = null) => AppendChild(children, index);

    public void AppendChild(IEnumerable<UIView> children, int? index = null)
    {
        if (index == null)
        {
            var changing = false;
            foreach (var child in children)
            {
                if (child?.Parent == this) continue;
                changing = true;
                child.Remove();
                Elements.Add(child);
                child.Parent = this;
            }

            if (!changing) return;
        }
        else if (index >= 0 || index <= Elements.Count)
        {
            var changing = false;
            var i = index.Value;
            foreach (var child in children)
            {
                if (child?.Parent == this) continue;
                changing = true;
                child.Remove();
                Elements.Insert(i++, child);
                child.Parent = this;
            }

            if (!changing) return;
        }
        else { return; }

        MarkLayoutDirty();
        MarkPositionDirty();

        ElementsOrderIsDirty = true;

        foreach (var child in children)
        {
            RuntimeHelper.ErrorCapture(() => { if (SilkyUI != null) child.HandleEnterTree(SilkyUI); });
            RuntimeHelper.ErrorCapture(child.Initialize);
        }
    }

    public void AppendChild(UIView child, int? index = null)
    {
        if (child?.Parent == this) return;

        if (index == null)
        {
            child.Remove();
            Elements.Add(child);
            child.Parent = this;
        }
        else if (index >= 0 || index <= Elements.Count)
        {
            child.Remove();
            Elements.Insert(index.Value, child);
            child.Parent = this;
        }
        else { return; }

        MarkLayoutDirty();
        MarkPositionDirty();

        ElementsOrderIsDirty = true;

        RuntimeHelper.ErrorCapture(() =>
        {
            if (SilkyUI != null) child.HandleEnterTree(SilkyUI);
        });
        RuntimeHelper.ErrorCapture(child.Initialize);
    }

    public int GetInnerChildIndex(UIView innerChild) => Elements.IndexOf(innerChild);
    public int GetInnerCachedChildIndex(UIView innerChild) => ElementsCache.IndexOf(innerChild);

    public virtual void RemoveChild(UIView child)
    {
        if (!Elements.Remove(child)) return;

        child.Parent = null;
        MarkLayoutDirty();
        MarkPositionDirty();
        ElementsOrderIsDirty = true;

        child.HandleExitTree();
    }

    public virtual void RemoveAllChildren()
    {
        foreach (var child in Elements.ToArray())
        {
            RemoveChild(child);
        }
    }

    #endregion

    public UIView SelectedElement { get; protected set; }

    public virtual void SelectChild(UIView selectTarget)
    {
        if (!ElementsCache.Contains(selectTarget)) return;

        SelectedElement?.HandleDeselected();
        SelectedElement = selectTarget;
        SelectedElement?.HandleSelected();
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        if (evt.Source != this && evt.Previous is { Selectable: true })
        {
            SelectChild(evt.Previous);
        }

        base.OnLeftMouseDown(evt);
    }

    #region Update UpdateStatus Draw

    public override void HandleUpdate(GameTime gameTime)
    {
        base.HandleUpdate(gameTime);
        UpdateChildren(gameTime);
    }

    protected virtual void UpdateChildren(GameTime gameTime)
    {
        foreach (var child in ElementsCache) child.HandleUpdate(gameTime);
    }

    public override void HandleUpdateStatus(GameTime gameTime)
    {
        base.HandleUpdateStatus(gameTime);
        UpdateChildrenStatus(gameTime);
    }

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

    public virtual Rectangle GetClippingRectangle(SpriteBatch spriteBatch)
    {
        var bounds = HiddenBox switch
        {
            HiddenBox.Outer => OuterBounds,
            HiddenBox.Inner => InnerBounds,
            _ => Bounds,
        };

        var topLeft = Vector2.Transform(bounds.Position, SilkyUI.TransformMatrix);
        var rightBottom = Vector2.Transform(bounds.RightBottom, SilkyUI.TransformMatrix);
        var rectangle = new Rectangle(
                (int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y),
                (int)Math.Ceiling(rightBottom.X - topLeft.X),
                (int)Math.Ceiling(rightBottom.Y - topLeft.Y));

        var scissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
        return Rectangle.Intersect(rectangle, scissorRectangle);
    }

    public virtual void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (OverflowHidden)
        {
            var innerBounds = InnerBounds;
            spriteBatch.End();
            var originalScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
            var scissor = Rectangle.Intersect(GetClippingRectangle(spriteBatch), originalScissor);
            spriteBatch.GraphicsDevice.ScissorRectangle = scissor;
            var deviceStatus = Main.graphics.GraphicsDevice.BackupStates(spriteBatch);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null, SilkyUI.TransformMatrix);

            foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(innerBounds)))
            {
                child.HandleDraw(gameTime, spriteBatch);
            }

            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = originalScissor;
            deviceStatus.Begin(spriteBatch, SpriteSortMode.Deferred);

            return;
        }

        foreach (var child in ElementsInOrder)
        {
            child.HandleDraw(gameTime, spriteBatch);
        }
    }

    #endregion

    protected readonly List<UIView> FreeChildren = [];
    protected readonly List<UIView> LayoutChildren = [];

    protected virtual void ClassifyChildren()
    {
        ElementsCache.Clear();
        ElementsCache.AddRange(Elements.Where(el => !el.Invalid));

        FreeChildren.Clear();
        LayoutChildren.Clear();

        foreach (var child in ElementsCache)
        {
            if (child.Positioning.IsFree)
            {
                FreeChildren.Add(child);
            }
            else
            {
                LayoutChildren.Add(child);
            }
        }

        if (LayoutChildren.Count <= 0)
        {
            FlexLines.Clear();
        }
    }

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