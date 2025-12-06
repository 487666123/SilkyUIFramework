namespace SilkyUIFramework;

[Service]
public class SilkyUIRegistrar
{
    public IReadOnlyDictionary<string, List<Type>> BodyTypesForGameUI { get; private set; }
    public IReadOnlyList<Type> BodyTypesForGlobalUI { get; private set; }

    public void RegisterUI(IEnumerable<Type[]> allTypes)
    {
        var bodyTypesForGameUI = new Dictionary<string, List<Type>>();
        var bodyTypeForGlobalUI = new List<Type>();

        foreach (var types in allTypes)
        {
            foreach (var (type, layer) in CollectGameUI(types))
            {
                var list = bodyTypesForGameUI.TryGetValue(layer, out var value)
                    ? value
                    : (bodyTypesForGameUI[layer] = []);
                list.Add(type);
            }

            bodyTypeForGlobalUI.AddRange(CollectGlobalUI(types));
        }

        BodyTypesForGlobalUI = bodyTypeForGlobalUI.AsReadOnly();
        BodyTypesForGameUI = bodyTypesForGameUI.AsReadOnly();
    }

    static IEnumerable<(Type, string)> CollectGameUI(Type[] types)
        => types.Where(t => t.IsSubclassOf(typeof(BaseBody)))
            .Select(t => (t, t.GetCustomAttribute<RegisterUIAttribute>()?.LayerNode))
            .Where(p => p.LayerNode != null);

    static IEnumerable<Type> CollectGlobalUI(Type[] types)
        => types.Where(t => t.IsSubclassOf(typeof(BaseBody)))
            .Where(t => t.GetCustomAttribute<RegisterGlobalUIAttribute>() != null);
}