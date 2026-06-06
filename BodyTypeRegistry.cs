using System.Collections.Immutable;

namespace SilkyUIFramework;

/// <summary>
/// 收集并分类已注册的 Body 类型，生成供 UI 系统使用的不可变注册表。
/// </summary>
[Service]
public class BodyTypeRegistry
{
    public static BodyTypeRegistry Instance => SilkyUISystem.ServiceProvider.GetRequiredService<BodyTypeRegistry>();

    /// <summary>
    /// 按原版界面层节点分组的游戏内 Body 类型。
    /// </summary>
    public ImmutableDictionary<string, ImmutableArray<Type>> GameBodyTypesByLayerNode { get; private set; } = [];

    /// <summary>
    /// 不依附于原版界面层的全局 Body 类型。
    /// </summary>
    public ImmutableArray<Type> GlobalBodyTypes { get; private set; } = [];

    /// <summary>
    /// 从已加载的 Mod 类型中收集 Body 类型，并生成只读注册结果。
    /// </summary>
    public void CollectFrom(ReadOnlySpan<ModLoadedTypes> modsWithLoadedTypes)
    {
        var gameBodyTypesByLayerNode = new Dictionary<string, List<Type>>();
        var globalBodyTypes = new List<Type>();

        foreach (var (_, types) in modsWithLoadedTypes)
        {
            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(BaseBody))) continue;

                if (type.GetCustomAttribute<RegisterUIAttribute>() is { LayerNode: { } layer })
                {
                    var list = gameBodyTypesByLayerNode.TryGetValue(layer, out var value)
                        ? value
                        : (gameBodyTypesByLayerNode[layer] = []);
                    list.Add(type);
                }

                if (type.GetCustomAttribute<RegisterGlobalUIAttribute>() != null)
                {
                    globalBodyTypes.Add(type);
                }
            }
        }

        GameBodyTypesByLayerNode = gameBodyTypesByLayerNode.ToImmutableDictionary(
            p => p.Key,
            p => p.Value.ToImmutableArray());
        GlobalBodyTypes = [.. globalBodyTypes];
    }
}
