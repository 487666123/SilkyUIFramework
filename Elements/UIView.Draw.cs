using SilkyUIFramework.Components;

namespace SilkyUIFramework.Elements;

public partial class UIView
{
    public HiddenBox HiddenBox { get; set; } = HiddenBox.Inner;

    public RectangleRender RectangleRender = new();

    public bool FinallyDrawBorder { get; set; }

    public Color BackgroundColor
    {
        get => RectangleRender.BackgroundColor;
        set => RectangleRender.BackgroundColor = value;
    }

    public Vector4 BorderRadius
    {
        get => RectangleRender.BorderRadius;
        set => RectangleRender.BorderRadius = value;
    }

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

    protected virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var position = Bounds.Position;
        var size = Bounds.Size;
        RectangleRender.DrawShadow(position, size, ref SilkyUI.TransformMatrix);
        RectangleRender.Draw(position, size, FinallyDrawBorder, ref SilkyUI.TransformMatrix);
    }

    public virtual void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        RuntimeSafeHelper.SafeInvoke(DrawAction, action => action(gameTime, spriteBatch));
        RuntimeSafeHelper.SafeInvoke(() => Draw(gameTime, spriteBatch));

        RectangleRender.DrawOnlyBorder(Bounds.Position, Bounds.Size, SilkyUI.TransformMatrix);
    }
}