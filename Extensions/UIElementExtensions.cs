namespace SilkyUIFramework.Extensions;

public static class UIElementExtensions
{
    public static T Join<T>(this T uie, UIElementGroup parent) where T : UIView
    {
        parent.AddChild(uie);
        return uie;
    }
}