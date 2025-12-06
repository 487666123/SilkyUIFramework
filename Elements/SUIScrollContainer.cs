namespace SilkyUIFramework.Elements;

public class SUIScrollContainer(SUIScrollView scrollView) : UIElementGroup
{
    public SUIScrollView ScrollView { get; } = scrollView;

    public void UpdateScrollPosition(Vector2 currentScrollPosition) => ScrollOffset = -currentScrollPosition;

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var innerBounds = ScrollView.Mask.InnerBounds;
        foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(innerBounds)))
        {
            child.HandleDraw(gameTime, spriteBatch);
        }
    }
}
