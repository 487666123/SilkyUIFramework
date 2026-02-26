namespace SilkyUIFramework.Helper;

public static class MatrixHelper
{
    /// <summary>
    /// <see cref="SpriteBatch"/> 矩阵变换为 SDF 矩阵
    /// </summary>
    public static Matrix Transform2SDFMatrix(Matrix matrix)
    {
        var device = Main.graphics.GraphicsDevice;
        var width = device.Viewport.Width;
        var height = device.Viewport.Height;

        matrix *= Matrix.CreateScale(2f / width, -2f / height, 1f);
        return matrix * Matrix.CreateTranslation(-1 + matrix.M41 * 2f / width, 1 - matrix.M42 * 2f / height, 0f);

        //matrix.M11 *= 2f / width;
        //matrix.M22 *= -2f / height;
        //matrix.M41 = -1 + matrix.M41 * 2f / width;
        //matrix.M42 = 1 - matrix.M42 * 2f / height;
    }
}
