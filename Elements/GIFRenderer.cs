using SixLabors.ImageSharp;

namespace SilkyUIFramework.Elements;

/// <summary>
/// 缩放，位置这些都还没做适配，代码一共就这么多。
/// </summary>
public class GifRenderer
{
    private readonly Texture2D[] _texture2Ds;
    private readonly int[] _frameDelays;
    private double _frameTimer;
    private int _currentFrame;

    public readonly int Width;
    public readonly int Height;

    public Vector2 Size => new(Width, Height);

    private GifRenderer(Texture2D[] texture2Ds, int[] frameDelays)
    {
        _texture2Ds = texture2Ds;
        _frameDelays = frameDelays;
        _frameTimer = 0;
        _currentFrame = 0;

        Width = _texture2Ds.Max(i => i.Width);
        Height = _texture2Ds.Max(i => i.Height);
    }

    /// <summary>
    /// 提取 Gif 帧数据
    /// </summary>
    /// <param name="gifBytes">原始数据</param>
    public static GifRenderer ExtractGifFrames(byte[] gifBytes)
    {
        // 解析图片数据
        using var image = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(gifBytes);

        var frameCount = image.Frames.Count;
        var texture2Ds = new Texture2D[frameCount];
        var frameDelays = new int[frameCount];

        for (var i = 0; i < frameCount; i++)
        {
            var frame = image.Frames[i];
            var gifMetadata = frame.Metadata.GetGifMetadata();

            // GIF frame delay is in 1/100th of a second, convert to milliseconds
            frameDelays[i] = gifMetadata.FrameDelay * 10;

            var texture = new Texture2D(Main.graphics.GraphicsDevice, frame.Width, frame.Height);
            var pixels = new byte[frame.Width * frame.Height * 4];
            Console.WriteLine($"size: {frame.Width}x{frame.Height}");
            frame.CopyPixelDataTo(pixels);
            texture.SetData(pixels);
            texture2Ds[i] = texture;
        }

        return new GifRenderer(texture2Ds, frameDelays);
    }

    /// <summary>
    /// 更新当前帧率
    /// </summary>
    /// <param name="gameTime"></param>
    public void Update(GameTime gameTime)
    {
        // Accumulate the elapsed time
        _frameTimer += gameTime.ElapsedGameTime.TotalMilliseconds;

        // Check if it's time to change frames
        while (_frameTimer >= _frameDelays[_currentFrame])
        {
            _frameTimer -= _frameDelays[_currentFrame];
            _currentFrame = (_currentFrame + 1) % _texture2Ds.Length;
        }
    }

    /// <summary>
    /// 绘制图像
    /// </summary>
    /// <param name="spriteBatch"></param>
    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        var device = Main.graphics.GraphicsDevice;
        var viewportSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
        spriteBatch.Draw(_texture2Ds[_currentFrame],
            position, null, Microsoft.Xna.Framework.Color.White, 0f, Size / 2, 0.5f, 0, 0f);
    }
}