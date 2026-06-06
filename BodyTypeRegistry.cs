using System.Collections.Immutable;

namespace SilkyUIFramework;

internal enum SilkyUIScope
{
    Global,
    GameLayer
}

internal sealed record SilkyUIRegistration(
    Type BodyType,
    string Name,
    int Priority,
    SilkyUIScope Scope,
    string LayerNode,
    InterfaceScaleType ScaleType);

/// <summary>
/// 收集并分类已注册的 Body 类型，生成供 UI 系统使用的不可变注册表。
/// </summary>
[Service]
public class BodyTypeRegistry
{
    public static BodyTypeRegistry Instance => SilkyUISystem.ServiceProvider.GetRequiredService<BodyTypeRegistry>();

    internal ImmutableDictionary<string, ImmutableArray<SilkyUIRegistration>> GameRegistrationsByLayerNode { get; private set; } = [];

    internal ImmutableArray<SilkyUIRegistration> GlobalRegistrations { get; private set; } = [];

    /// <summary>
    /// 从已加载的 Mod 类型中收集 Body 类型，并生成只读注册结果。
    /// </summary>
    public void CollectFrom(ReadOnlySpan<ModLoadedTypes> modsWithLoadedTypes)
    {
        var gameRegistrationsByLayerNode = new Dictionary<string, List<SilkyUIRegistration>>();
        var globalRegistrations = new List<SilkyUIRegistration>();

        foreach (var (_, types) in modsWithLoadedTypes)
        {
            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(BaseBody))) continue;

                if (type.GetCustomAttribute<RegisterUIAttribute>() is { LayerNode: { } layer } registerUI)
                {
                    var registrations = gameRegistrationsByLayerNode.TryGetValue(layer, out var registrationList)
                        ? registrationList
                        : (gameRegistrationsByLayerNode[layer] = []);
                    registrations.Add(new SilkyUIRegistration(
                        type,
                        registerUI.Name,
                        registerUI.Priority,
                        SilkyUIScope.GameLayer,
                        registerUI.LayerNode,
                        registerUI.InterfaceScaleType));
                }

                if (type.GetCustomAttribute<RegisterGlobalUIAttribute>() is { } registerGlobalUI)
                {
                    globalRegistrations.Add(new SilkyUIRegistration(
                        type,
                        registerGlobalUI.Name,
                        registerGlobalUI.Priority,
                        SilkyUIScope.Global,
                        null,
                        InterfaceScaleType.UI));
                }
            }
        }

        GameRegistrationsByLayerNode = gameRegistrationsByLayerNode.ToImmutableDictionary(
            p => p.Key,
            p => p.Value.ToImmutableArray());
        GlobalRegistrations = [.. globalRegistrations];
    }
}
