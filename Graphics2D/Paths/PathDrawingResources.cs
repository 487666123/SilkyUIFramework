namespace SilkyUIFramework.Graphics2D.Paths;

[Service]
internal sealed class PathDrawingResources : IDisposable
{
    private readonly Dictionary<GraphicsDevice, PathRenderer> _renderers = new();

    internal PathRenderer GetRenderer(GraphicsDevice device)
    {
        if (!_renderers.TryGetValue(device, out var renderer))
        {
            renderer = new PathRenderer(device);
            _renderers.Add(device, renderer);
        }
        return renderer;
    }

    public void Dispose()
    {
        foreach (var renderer in _renderers.Values) renderer.Dispose();
        _renderers.Clear();
    }
}
