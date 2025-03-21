﻿using SilkyUIFramework.MyElement;

namespace SilkyUIFramework;

public partial class UIElementGroup
{
    protected internal virtual void OnChildDirty()
    {
        if (IsDirty) return;
        if (AutomaticWidth || AutomaticHeight)
        {
            IsDirty = true;
            Parent?.OnChildDirty();
            return;
        }
        IsDirty = true;
    }

    public override void UpdateBounds()
    {
        if (IsDirty)
        {
            Measure(GetParentAvailableSpace());
            Trim();
            ApplyLayout();

            CleanupDirtyMark();
        }

        foreach (var child in GetValidChildren())
        {
            child.UpdateBounds();
        }
    }

    public override void UpdatePosition()
    {
        base.UpdatePosition();

        foreach (var child in GetValidChildren())
        {
            child.UpdatePosition();
        }
    }

    public override void RecalculatePosition()
    {
        base.RecalculatePosition();

        foreach (var child in LayoutChildren)
        {
            child.RecalculatePosition();
        }
    }
}