namespace SilkyUIFramework;

[Service]
public class SilkyUIRegistrar
{
    readonly Dictionary<string, List<Type>> _gameUIBodyTypesByLayer = [];
    readonly List<Type> _globalUIBodyTypes = [];

    public IReadOnlyDictionary<string, List<Type>> GameUIBodyTypesByLayer => _gameUIBodyTypesByLayer;
    public IReadOnlyList<Type> GlobalUIBodyTypes => _globalUIBodyTypes;

    public void CollectFrom(ReadOnlySpan<ModLoadedTypes> modsWithLoadedTypes)
    {
        foreach (var (_, types) in modsWithLoadedTypes)
        {
            foreach (var (type, layer) in CollectGameUIBodyTypes(types))
            {
                var list = _gameUIBodyTypesByLayer.TryGetValue(layer, out var value)
                    ? value
                    : (_gameUIBodyTypesByLayer[layer] = []);
                list.Add(type);
            }

            _globalUIBodyTypes.AddRange(CollectGlobalUIBodyTypes(types));
        }
    }

    static IEnumerable<(Type, string)> CollectGameUIBodyTypes(Type[] types)
        => types.Where(t => t.IsSubclassOf(typeof(BaseBody)))
            .Select(t => (t, t.GetCustomAttribute<RegisterUIAttribute>()?.LayerNode))
            .Where(p => p.LayerNode != null);

    static IEnumerable<Type> CollectGlobalUIBodyTypes(Type[] types)
        => types.Where(t => t.IsSubclassOf(typeof(BaseBody)))
            .Where(t => t.GetCustomAttribute<RegisterGlobalUIAttribute>() != null);
}