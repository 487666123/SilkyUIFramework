using SilkyUIFramework.Components;

namespace SilkyUIFramework.Elements;

public partial class UIView
{
    /// <summary>
    /// 控制当前元素作为容器时，裁剪子元素所使用的盒模型区域。
    /// </summary>
    public HiddenBox HiddenBox { get; set; } = HiddenBox.Inner;

    /// <summary>
    /// 当前元素的矩形渲染器，负责背景、边框与阴影样式。
    /// </summary>
    public readonly RectangleRender RectangleRender = new();

    /// <summary>
    /// 背景色（代理到 <see cref="RectangleRender.BackgroundColor"/>）。
    /// </summary>
    public Color BackgroundColor
    {
        get => RectangleRender.BackgroundColor;
        set => RectangleRender.BackgroundColor = value;
    }

    /// <summary>
    /// 圆角半径（代理到 <see cref="RectangleRender.BorderRadius"/>）。
    /// </summary>
    public Vector4 BorderRadius
    {
        get => RectangleRender.BorderRadius;
        set => RectangleRender.BorderRadius = value;
    }

    /// <summary>
    /// 边框颜色（代理到 <see cref="RectangleRender.BorderColor"/>）。
    /// </summary>
    public Color BorderColor
    {
        get => RectangleRender.BorderColor;
        set => RectangleRender.BorderColor = value;
    }

    /// <summary>
    /// 与 <see cref="Update(GameTime)"/> 不同，此方法在 <see cref="Draw(GameTime, SpriteBatch)"/> 方法之前调用，用于更新动画相关状态
    /// </summary>
    protected virtual void UpdateStatus(GameTime gameTime)
    {
        if (IsMouseHovering)
        {
            if (!HoverTimer.IsForward)
                HoverTimer.StartUpdate();
        }
        else
        {
            if (!HoverTimer.IsReverse)
                HoverTimer.StartReverseUpdate();
        }

        HoverTimer.Update(gameTime);
    }

    /// <summary>
    /// 绘制当前元素的阴影与主体矩形。
    /// </summary>
    protected virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var position = Bounds.Position;
        var size = Bounds.Size;
        RectangleRender.DrawShadow(position, size, ref SilkyUI.TransformMatrix);
        RectangleRender.Draw(position, size, ref SilkyUI.TransformMatrix);
    }

    /// <summary>
    /// 当前元素绘制入口，默认直接执行 <see cref="Draw(GameTime, SpriteBatch)"/>。
    /// </summary>
    public virtual void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Draw(gameTime, spriteBatch);
    }
}
