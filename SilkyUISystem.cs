using Terraria.ModLoader.Core;

namespace SilkyUIFramework;

public record ModLoadedTypes(Mod Mod, Type[] LoadedTypes);

public partial class SilkyUISystem : ModSystem
{
    public static SilkyUISystem Instance => ModContent.GetInstance<SilkyUISystem>();
    public static IServiceProvider ServiceProvider { get; private set; }

    public SilkyUIManager SilkyUIManager { get; private set; }
    private SilkyUIRegistrar SilkyUIRegistrar { get; set; }

    private ModLoadedTypes[] _modsWithLoadedTypes;


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
        CollectLoadedTypes();

        ServiceProvider = ServiceProviderBuilder.BuildServiceProvider(_modsWithLoadedTypes);

        SilkyUIManager = ServiceProvider.GetRequiredService<SilkyUIManager>();
        SilkyUIRegistrar = ServiceProvider.GetRequiredService<SilkyUIRegistrar>();
    }

    public override void Unload() => ServiceProvider = null;

    public override void PostSetupContent()
    {
        SilkyUIRegistrar.CollectFrom(_modsWithLoadedTypes);
        SilkyUIManager.Initialize();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) =>
        SilkyUIManager.ModifyInterfaceLayers(layers);

    public override void PreSaveAndQuit()
    {

    }
}

public class SilkyUIPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (SilkyUIRenderSystem.Instance is { } rs) rs.ReloadSilkyUIStacks();
    }
}
