using log4net;
using MonoMod.Cil;

namespace SilkyUIFramework.Hooks;

class UIHookInstaller : ILoadable
{
    static ILog _logger;
    public void Load(Mod mod)
    {
        _logger = mod.Logger;
        On_Main.UpdateUIStates += (orig, self) =>
        {
            SilkyUISystem.Instance?.SilkyUIManager?.Update(Main.gameTimeCache);
            orig(self);
        };

        On_Main.DrawThickCursor += (orig, smart) =>
        {
            SilkyUISystem.Instance?.SilkyUIManager?.Draw(Main.gameTimeCache, Main.spriteBatch);
            return orig(smart);
        };

        IL_Main.DoDraw += static (il) =>
        {
            var c = new ILCursor(il);

            c.EmitDelegate(() =>
            {
                try { SilkyUISystem.Instance?.SilkyUIManager?.HandleIME(); }
                catch (Exception ex) { _logger.Error(ex); }
            });
        };
    }

    public void Unload() { }
}
