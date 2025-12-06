namespace SilkyUIFramework.Extensions;

public static class ViewportExtensions
{
    extension(Rectangle rectangle)
    {
        public Vector2 Position => new(rectangle.X, rectangle.Y);
        public Vector2 Size => new(rectangle.Width, rectangle.Height);
    }

    extension(Viewport viewport)
    {
        public Vector2 Position => new(viewport.X, viewport.Y);
        public Vector2 Size => new(viewport.Width, viewport.Height);

        public Rectangle Rectangle => new(viewport.X, viewport.Y, viewport.Width, viewport.Height);

        public Viewport IncreaseSize(int width, int height)
        {
            viewport.Width += width;
            viewport.Height += height;
            return viewport;
        }

        public Viewport WithXy(int x, int y)
        {
            viewport.X = x;
            viewport.Y = y;
            return viewport;
        }

        public Viewport WithXy(Viewport other)
        {
            viewport.X = other.X;
            viewport.Y = other.Y;
            return viewport;
        }

        public Viewport WithXy(Viewport other, int x, int y)
        {
            viewport.X = other.X + x;
            viewport.Y = other.Y + y;
            return viewport;
        }
    }
}
