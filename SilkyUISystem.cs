using Terraria.ModLoader.Core;

namespace SilkyUIFramework;

public partial class SilkyUISystem : ModSystem
{
    public static SilkyUISystem Instance => ModContent.GetInstance<SilkyUISystem>();

    public static IServiceProvider ServiceProvider { get; private set; }

    public SilkyUIManager SilkyUIManager { get; private set; }
    private SilkyUIRegistrar SilkyUIRegistrar { get; set; }

    private IReadOnlyList<Assembly> Assemblies { get; set; }
    private IEnumerable<Type[]> GetLoadableTypes() => Assemblies.Select(AssemblyManager.GetLoadableTypes);

    public override void Load()
    {
        Assemblies = [.. ModLoader.Mods.Select(m => m.Code)];

        ServiceProvider = ServiceProviderBuilder.BuildServiceProvider(GetLoadableTypes());

        SilkyUIManager = ServiceProvider.GetRequiredService<SilkyUIManager>();
        SilkyUIRegistrar = ServiceProvider.GetRequiredService<SilkyUIRegistrar>();
    }

    public override void Unload()
    {
        ServiceProvider = null;
    }

    public override void PostSetupContent()
    {
        SilkyUIRegistrar.RegisterUI(GetLoadableTypes());
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
        if (SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUIRenderSystem>() is not { } renderSystem) return;

        renderSystem.ReloadGameGroups();
    }
}