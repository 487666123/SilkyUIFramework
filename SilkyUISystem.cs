using SilkyUIFramework.Interfaces;
using Terraria.ModLoader.Core;

namespace SilkyUIFramework;

public record ModLoadedTypes(Mod Mod, Type[] LoadedTypes);

public partial class SilkyUISystem : ModSystem
{
    public static SilkyUISystem Instance => ModContent.GetInstance<SilkyUISystem>();
    public static IServiceProvider ServiceProvider { get; private set; }

    public SilkyUIManager SilkyUIManager { get; private set; }
    SilkyUIRegistrar SilkyUIRegistrar { get; set; }

    ModLoadedTypes[] _modsWithLoadedTypes;

    public ISilkyUIAssetProvider AssetProvider { get; private set; }

    /// <summary>
    /// 收集 Mod 已加载的类型
    /// </summary>
    void CollectLoadedTypes()
    {
        var mods = ModLoader.Mods.AsSpan();


        bool containsSUI = false;
        for (int i = 0; i < mods.Length; i++)
        {
            if (mods[i].Name is nameof(SilkyUIFramework)) 
            {
                containsSUI = true;
                break;
            }
        }

        _modsWithLoadedTypes = new ModLoadedTypes[containsSUI ? mods.Length : (mods.Length + 1)];


        for (int i = 0; i < mods.Length; i++)
        {
            var mod = mods[i];
            _modsWithLoadedTypes[i] = new ModLoadedTypes(mod, AssemblyManager.GetLoadableTypes(mod.Code));
        }

        if (!containsSUI) 
        {
            var mod = ModContent.GetInstance<SilkyUIFramework>();
            _modsWithLoadedTypes[^1] = new ModLoadedTypes(mod, AssemblyManager.GetLoadableTypes(mod.Code));
        }
    }

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server) return;
        
        CollectLoadedTypes();

        ServiceProvider = ServiceProviderBuilder.BuildServiceProvider(_modsWithLoadedTypes);

        AssetProvider = ServiceProvider.GetService<ISilkyUIAssetProvider>() ?? new SilkyUIAssetProvider();

        SilkyUIManager = ServiceProvider.GetRequiredService<SilkyUIManager>();
        SilkyUIRegistrar = ServiceProvider.GetRequiredService<SilkyUIRegistrar>();
    }

    public override void Unload() => ServiceProvider = null;

    public override void PostSetupContent()
    {
        if (Main.netMode == NetmodeID.Server) return;

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
