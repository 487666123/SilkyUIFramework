global using SilkyUIFramework;

namespace SilkyUIFramework;

public enum MouseButtonType : int
{
    Left,
    Middle,
    Right
}

public enum LayoutType
{
    Flexbox,
    Grid,
    Custom,
}

public enum HiddenBox
{
    Outer,
    Middle,
    Inner,
}

public enum BoxSizing
{
    /// <summary>
    /// 控制边框, 内边距, 内容
    /// </summary>
    Border = 0,

    /// <summary>
    /// 控制内容区域
    /// </summary>
    Content = 1
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

public enum Positioning
{
    Relative,
    Absolute,
    Sticky,
    Fixed,
    Static,
}

public static class PositioningExtensions
{
    extension(Positioning positioning)
    {
        public bool IsOutOfFlow => positioning is Positioning.Fixed or Positioning.Absolute;
    }
}