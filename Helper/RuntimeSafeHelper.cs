using System.Diagnostics;
using log4net;

namespace SilkyUIFramework.Helper;

public static class RuntimeSafeHelper
{
    public static bool StrictMode { get; set; } = false;

    private static ILog Logger
    {
        get
        {
            if (field != null) return field;
            return field = ModContent.GetInstance<SilkyUIFramework>()?.Logger;
        }
    }

    public static void SafeInvoke<T>(T actions, Action<T> action) where T : Delegate
    {
        if (actions == null || action == null) return;

        foreach (var @delegate in actions.GetInvocationList().OfType<T>())
        {
            var delegate1 = @delegate;
            SafeInvoke(() => action(delegate1));
        }
    }

    public static void SafeInvoke(Action action)
    {
        if (action is null) return;

        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            Logger?.Error("RuntimeHelper ErrorCapture", ex);
            if (Debugger.IsAttached && StrictMode) throw;
        }
    }
}