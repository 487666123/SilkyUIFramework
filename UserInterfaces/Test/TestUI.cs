#if DEBUG && false

using SilkyUIFramework.Animation;

namespace SilkyUIFramework.UserInterfaces.Test;

[RegisterUI("Vanilla: Radial Hotbars", "TestUI")]
public partial class TestUI : BaseBody
{
    protected override void OnInitialize()
    {
        InitializeComponent();

        // 添加十个 Image 元素
        for (int i = 0; i < 10; i++)
        {
            // 传入参数
            var image = new SUIImage(TextureAssets.Item[1]).Join(Container);
            image.ImageAlign = new Vector2(0.5f, 0.5f);

            // 默认会 Fit 大小，这里给个固定的
            image.FitWidth = false;
            image.FitHeight = false;
            image.SetSize(50, 50);

            image.MouseEnter += (source, e) =>
            {
                ItemHoverTimer.StartUpdate(true);
                // 鼠标进入 image 元素时候设置悬浮元素的位置
                HoverView.DragOffset = image.LayoutOffset + image.Bounds.Size / 2f;
            };

            image.MouseLeave += (source, e) =>
            {
                ItemHoverTimer.StartReverseUpdate();
            };
        }
    }

    private readonly AnimationTimer ItemHoverTimer = new();

    protected override void UpdateStatus(GameTime gameTime)
    {
        ItemHoverTimer.Update(gameTime);

        HoverView.SetWidth(ItemHoverTimer.Lerp(50f, 62f));
        HoverView.SetHeight(ItemHoverTimer.Lerp(50f, 62f));
        HoverView.BorderRadius = new Vector4(ItemHoverTimer.Lerp(4f, 10f));
        HoverView.BorderColor = ItemHoverTimer.Lerp(Color.Transparent, Color.Red * 0.5f);

        base.UpdateStatus(gameTime);
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }
}

#endif