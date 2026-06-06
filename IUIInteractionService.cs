namespace SilkyUIFramework;

public interface IUIInteractionService
{
    UIView HitTest(Vector2 position);
    void Activate(UIView element);
}
