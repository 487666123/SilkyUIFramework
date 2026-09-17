namespace SilkyUIFramework.StyleSystem;

/// <summary>可组合的元素状态。样式优先级由 UIStyleSheet 定义，与标志数值无关。</summary>
[Flags]
public enum UIElementState
{
    /// <summary>基础样式，始终参与合并。</summary>
    Normal = 0,

    /// <summary>鼠标悬停。</summary>
    Hover = 1 << 0,

    /// <summary>激活，通常表示鼠标按下。</summary>
    Active = 1 << 1,

    /// <summary>获得焦点。</summary>
    Focus = 1 << 2,

    /// <summary>禁用。</summary>
    Disabled = 1 << 3,

    /// <summary>选中。</summary>
    Selected = 1 << 4
}
