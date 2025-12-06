namespace SilkyUIFramework.Extensions;

public static class Vector2Extensions
{
    extension(Vector2 vector2)
    {
        public Vector2 AddX(float x)
        {
            vector2.X += x;
            return vector2;
        }

        public Vector2 AddY(float y)
        {
            vector2.Y += y;
            return vector2;
        }

        public Vector2 Add(float x, float y)
        {
            vector2.X += x;
            vector2.Y += y;
            return vector2;
        }
    }
}
