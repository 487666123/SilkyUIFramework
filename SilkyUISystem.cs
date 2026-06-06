using Terraria.ModLoader.Core;

namespace SilkyUIFramework;

public record ModLoadedTypes(Mod Mod, Type[] LoadedTypes);

public partial class SilkyUISystem : ModSystem
{
    public static SilkyUISystem Instance => ModContent.GetInstance<SilkyUISystem>();
    public static IServiceProvider ServiceProvider { get; private set; }

    public SilkyUIManager SilkyUIManager { get; private set; }
    BodyTypeRegistry BodyTypeRegistry { get; set; }

    ModLoadedTypes[] _modsWithLoadedTypes;

    /// <summary>
    /// 收集 Mod 已加载的类型
    /// </summary>
    void CollectLoadedTypes()
    {
        var mods = ModLoader.Mods.AsSpan();
        _modsWithLoadedTypes = new ModLoadedTypes[mods.Length];

        for (int i = 0; i < mods.Length; i++)
        {
            var mod = mods[i];
            _modsWithLoadedTypes[i] = new ModLoadedTypes(mod, AssemblyManager.GetLoadableTypes(mod.Code));
        }
    }

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server) return;

        CollectLoadedTypes();

        ServiceProvider = ServiceProviderBuilder.BuildServiceProvider(_modsWithLoadedTypes);

        SilkyUIManager = ServiceProvider.GetRequiredService<SilkyUIManager>();
        BodyTypeRegistry = ServiceProvider.GetRequiredService<BodyTypeRegistry>();
    }

    public override void Unload()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();

        ServiceProvider = null;
        SilkyUIManager = null;
        BodyTypeRegistry = null;
        _modsWithLoadedTypes = null;
    }

    public override void PostSetupContent()
    {
        if (Main.netMode == NetmodeID.Server) return;

        BodyTypeRegistry.CollectFrom(_modsWithLoadedTypes);
        SilkyUIManager.Initialize();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) =>
        SilkyUIManager.ModifyInterfaceLayers(layers);
}

public class SilkyUIPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        SilkyUISystem.ServiceProvider?.GetService<SilkyUIManager>()?.ReloadGameScenes();
    }
}
