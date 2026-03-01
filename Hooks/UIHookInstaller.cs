using MonoMod.Cil;

namespace SilkyUIFramework.Hooks;

internal class UIHookInstaller : ILoadable
{
    public void Load(Mod mod)
    {
        On_Main.UpdateUIStates += (orig, self) =>
        {
            SilkyUISystem.Instance?.SilkyUIManager?.Update(Main.gameTimeCache);
            orig(self);
        };

        On_Main.DrawThickCursor += (orig, smart) =>
        {
            SilkyUISystem.Instance?.SilkyUIManager?.Draw(Main.gameTimeCache);
            return orig(smart);
        };

        //On_Main.DoDraw += (orig, self, gameTime) =>
        //{
        //    RuntimeSafeHelper.SafeInvoke(static delegate { SilkyUISystem.Instance?.SilkyUIManager?.HandleIME(); });
        //    orig(self, gameTime);
        //};

        IL_Main.DoDraw += (ILContext il) =>
        {
            var c = new ILCursor(il);

            c.EmitDelegate(() =>
            {
                try { SilkyUISystem.Instance?.SilkyUIManager?.HandleIME(); } catch { }
            });
        };

        //On_Main.DrawInterface += (orig, self, gametime) =>
        //{
        //    orig(self, gametime);
        //};

        //IL_Main.DrawMenu += il =>
        //{
        //    var c = new ILCursor(il);

        //    if (!c.TryGotoNext(MoveType.Before, i => i.MatchCall<Main>(nameof(Main.DrawThickCursor)))) return;
        //    if (!c.TryGotoPrev(MoveType.Before, i => i.MatchLdcI4(0))) return;

        //    SilkyUIFramework.Instance.Logger?.Info("IL_Main.DrawMenu success!");

        //    c.EmitDelegate(HandleInput);
        //};
    }

    public void Unload() { }
}
