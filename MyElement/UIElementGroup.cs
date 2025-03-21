﻿using SilkyUIFramework.MyElement;

namespace SilkyUIFramework;

/// <summary>
/// 似乎在密谋着什么，再等等...
/// </summary>
public partial class UIElementGroup : UIView
{
    public bool OverflowHidden { get; set; }

    protected List<UIView> Elements { get; } = [];
    public IEnumerable<UIView> GetValidChildren() => Elements.Where(el => !el.Invalid);
    public IReadOnlyList<UIView> Children => Elements;

    /// <summary>
    /// 尺寸与布局完成后, 清理脏标记 (只会清理 <see cref="LayoutChildren"/>)
    /// </summary>
    public override void CleanupDirtyMark()
    {
        base.CleanupDirtyMark();

        // 仅针对
        foreach (var child in LayoutChildren)
        {
            child.CleanupDirtyMark();
        }
    }

    #region Append Remove RemoveChild

    public virtual void AppendChild(UIView child)
    {
        child.Remove();
        Elements.Add(child);
        child.Parent = this;
        MarkDirty();
        PositionDirty = true;
    }

    public virtual void RemoveChild(UIView child)
    {
        if (!Elements.Remove(child)) return;

        child.Parent = null;
        MarkDirty();
        PositionDirty = true;
    }

    public virtual void RemoveAllChildren()
    {
        foreach (var child in Elements)
        {
            child.Parent = null;
        }

        Elements.Clear();
        MarkDirty();
        PositionDirty = true;
    }

    #endregion

    #region Update UpdateStatus Draw

    public override void HandleUpdate(GameTime gameTime)
    {
        base.HandleUpdate(gameTime);
        UpdateChildren(gameTime);
    }

    protected virtual void UpdateChildren(GameTime gameTime)
    {
        foreach (var child in GetValidChildren()) child.HandleUpdate(gameTime);
    }

    public override void HandleUpdateStatus(GameTime gameTime)
    {
        base.HandleUpdateStatus(gameTime);
        UpdateChildrenStatus(gameTime);
    }

    protected virtual void UpdateChildrenStatus(GameTime gameTime)
    {
        foreach (var child in GetValidChildren())
        {
            child.HandleUpdateStatus(gameTime);
        }
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
        DrawChildren(gameTime, spriteBatch);
    }

    public virtual void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        foreach (var child in GetValidChildren())
        {
            child.HandleDraw(gameTime, spriteBatch);
        }
    }

    #endregion

    protected readonly List<UIView> FreeChildren = [];
    protected readonly List<UIView> LayoutChildren = [];

    protected void ClassifyElements()
    {
        FreeChildren.Clear();
        LayoutChildren.Clear();

        foreach (var child in GetValidChildren())
        {
            if (child.Positioning.IsFree())
            {
                FreeChildren.Add(child);
            }
            else
            {
                LayoutChildren.Add(child);
            }
        }
    }

    public override UIView GetElementAt(Vector2 mousePosition)
    {
        if (Invalid) return null;

        // 开启溢出隐藏后, 需要先检查自身是否包含点
        if (OverflowHidden)
        {
            if (!ContainsPoint(mousePosition)) return null;

            foreach (var child in GetValidChildren())
            {
                var target = child.GetElementAt(mousePosition);
                if (target != null) return target;
            }

            return IgnoreMouseInteraction ? null : this;
        }

        foreach (var child in GetValidChildren())
        {
            if (child.GetElementAt(mousePosition) is { } target) return target;
        }

        // 忽略鼠标交互
        if (IgnoreMouseInteraction) return null;

        // 元素包含点
        return ContainsPoint(mousePosition) ? this : null;
    }

    public Vector2 ScrollOffset
    {
        get => _scrollOffset;
        protected set
        {
            if (value == _scrollOffset) return;
            _scrollOffset = value;
            PositionDirty = true;
        }
    }

    private Vector2 _scrollOffset;
}