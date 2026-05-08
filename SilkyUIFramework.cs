using SilkyUIFramework.Hooks;
using System.Runtime.Loader;

namespace SilkyUIFramework;

public class SilkyUIFramework : Mod
{
    public static SilkyUIFramework Instance => ModContent.GetInstance<SilkyUIFramework>();


    public static void DllRefAddcontent(Mod mod)
    {
        mod.GetFileBytes($"lib/{nameof(SilkyUIFramework)}.dll");
        var suiInstance = new SilkyUIFramework()
        {
            Code = typeof(SilkyUIFramework).Assembly
        };
        ContentInstance.Register(suiInstance);
        mod.AddContent<BlurMakeSystem>();
        mod.AddContent<UIHookInstaller>();
        mod.AddContent<SilkyUIPlayer>();
        mod.AddContent<SilkyUISystem>();
    }
}