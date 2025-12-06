namespace SilkyUIFramework.Helper;

public static class PlayerInputHelper
{
    public static void SetZoom(Matrix matrix) => SetZoom(in matrix);

    public static void SetZoom(in Matrix matrix) => SetZoom(new Vector2(1f / matrix.M11, 1f / matrix.M22));

    public static void SetZoom(Vector2 scale)
    {
        Main.lastMouseX = (int)(PlayerInput._originalLastMouseX * scale.X);
        Main.lastMouseY = (int)(PlayerInput._originalLastMouseY * scale.Y);
        Main.mouseX = (int)(PlayerInput._originalMouseX * scale.X);
        Main.mouseY = (int)(PlayerInput._originalMouseY * scale.Y);
        Main.screenWidth = (int)(PlayerInput._originalScreenWidth * scale.X);
        Main.screenHeight = (int)(PlayerInput._originalScreenHeight * scale.Y);
    }
}