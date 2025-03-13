﻿namespace SilkyUIFramework.Core;

public enum Positioning
{
    Static,
    Relative,
    Absolute,
    Sticky,
    Fixed,
}

[Flags]
public enum StickyType
{
    Left = 1 << 0,
    Top = 1 << 1,
    Right = 1 << 2,
    Bottom = 1 << 3,
    LeftTop = Left | Top,
    RightTop = Right | Top,
    LeftBottom = Left | Bottom,
    RightBottom = Right | Bottom,
}